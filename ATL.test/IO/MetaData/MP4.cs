﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ATL.AudioData;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Drawing.Imaging;

namespace ATL.test.IO.MetaData
{
    /*
     * IMPLEMENTED USE CASES
     *  
     *  1. Single metadata fields
     *                                Read  | Add   | Remove
     *  Supported textual field     |   x   |  x    | x
     *  Unsupported textual field   |   x   |  x    | x
     *  Supported picture           |   x   |  x    | x
     *  Unsupported picture         |   x   |  x    | x
     *  
     *  2. General behaviour
     *  
     *  Whole tag removal
     *  
     *  Conservation of unmodified tag items after tag editing
     *  Conservation of unsupported tag field after tag editing
     *  Conservation of supported pictures after tag editing
     *  Conservation of unsupported pictures after tag editing
     *  
     *  3. Specific behaviour
     *  
     *  Remove single supported picture (from normalized type and index)
     *  Remove single unsupported picture (with multiple pictures; checking if removing pic 2 correctly keeps pics 1 and 3)
     *  
     *  4. Technical
     *  
     *  Cohabitation with ID3v1/ID3v2/APE (=> no impact on MP4 internals)
     *
     */

    /*
     * TODO
     * 
     * FUNCTIONAL
     * 
     * Individual picture removal (from index > 1)
     * 
     * 
     * TECHNICAL
     * 
     * Exact picture data conservation after tag editing
     * Files with mdat located before moov
     * 
    */


    [TestClass]
    public class MP4 : MetaIOTest
    {
        public MP4()
        {
            emptyFile = "AAC/empty.m4a";
            notEmptyFile = "AAC/mp4.m4a";
            tagType = MetaDataIOFactory.TAG_NATIVE;

            // MP4 does not support leading zeroes
            testData.TrackNumber = "1";
            testData.TrackTotal = "2";
            testData.DiscNumber = "3";
            testData.DiscTotal = "4";
            testData.Conductor = null; // TODO - Should be supported; extended field makes it harder to manipulate by the generic test code
            testData.Publisher = null;
            testData.RecordingDate = null;
            testData.Genre = "Household"; // "House" was generating a 'gnre' numeric field whereas ATL standard way of tagging is '(c)gen' string field => Start with a non-standard Genre

            testData.AdditionalFields.Clear();
            testData.AdditionalFields.Add(new MetaFieldInfo(MetaDataIOFactory.TAG_ANY, "----:com.apple.iTunes:TEST", "xxx"));

            testData.Pictures.Clear();
            PictureInfo pic = new PictureInfo(Commons.ImageFormat.Jpeg, MetaDataIOFactory.TAG_ANY, 13);
            byte[] data = System.IO.File.ReadAllBytes(TestUtils.GetResourceLocationRoot() + "_Images/pic1.jpeg");
            pic.PictureData = data;
            pic.ComputePicHash();
            testData.Pictures.Add(pic);
        }


        [TestMethod]
        public void TagIO_R_MP4()
        {
            ConsoleLogger log = new ConsoleLogger();

            // Source : MP3 with existing tag incl. unsupported picture (Cover Art (Fronk)); unsupported field (MOOD)
            String location = TestUtils.GetResourceLocationRoot() + notEmptyFile;
            AudioDataManager theFile = new AudioDataManager(AudioData.AudioDataIOFactory.GetInstance().GetFromPath(location));

            readExistingTagsOnFile(theFile, 1);
        }

        [TestMethod]
        public void TagIO_RW_MP4_Empty()
        {
            test_RW_Empty(emptyFile, true, true, true);
        }

        [TestMethod]
        public void TagIO_RW_MP4_Existing()
        {
            ConsoleLogger log = new ConsoleLogger();

            // Source : MP3 with existing tag incl. unsupported picture (Cover Art (Fronk)); unsupported field (MOOD)
            String testFileLocation = TestUtils.CopyAsTempTestFile(notEmptyFile);
            AudioDataManager theFile = new AudioDataManager(AudioData.AudioDataIOFactory.GetInstance().GetFromPath(testFileLocation));

            // Add a new supported field and a new supported picture
            Assert.IsTrue(theFile.ReadFromFile());

            TagData theTag = new TagData();
            theTag.Conductor = "John Jackman";

            PictureInfo picInfo = new PictureInfo(Commons.ImageFormat.Png, PictureInfo.PIC_TYPE.Generic, MetaDataIOFactory.TAG_ANY, 14);
            picInfo.PictureData = File.ReadAllBytes(TestUtils.GetResourceLocationRoot() + "_Images/pic1.png");
            theTag.Pictures.Add(picInfo);


            // Add the new tag and check that it has been indeed added with all the correct information
            Assert.IsTrue(theFile.UpdateTagInFile(theTag, MetaDataIOFactory.TAG_NATIVE));

            readExistingTagsOnFile(theFile, 2);

            // Additional supported field
            Assert.AreEqual("John Jackman", theFile.NativeTag.Conductor);

            byte nbFound = 0;
            foreach (PictureInfo pic in theFile.NativeTag.EmbeddedPictures)
            {
                if (pic.PicType.Equals(PictureInfo.PIC_TYPE.Generic) && (1 == nbFound))
                {
                    Image picture = Image.FromStream(new MemoryStream(pic.PictureData));
                    Assert.AreEqual(picture.RawFormat, System.Drawing.Imaging.ImageFormat.Png);
                    Assert.AreEqual(picture.Width, 175);
                    Assert.AreEqual(picture.Height, 168);
                }
                nbFound++;
            }

            Assert.AreEqual(2, nbFound);

            // Remove the additional supported field
            theTag = new TagData();
            theTag.Conductor = "";

            // Remove additional picture
            picInfo = new PictureInfo(Commons.ImageFormat.Jpeg, PictureInfo.PIC_TYPE.Back);
            picInfo.MarkedForDeletion = true;
            theTag.Pictures.Add(picInfo);

            // Add the new tag and check that it has been indeed added with all the correct information
            Assert.IsTrue(theFile.UpdateTagInFile(theTag, MetaDataIOFactory.TAG_NATIVE));

            readExistingTagsOnFile(theFile);

            // Additional removed field
            Assert.AreEqual("", theFile.NativeTag.Conductor);


            // Check that the resulting file (working copy that has been tagged, then untagged) remains identical to the original file (i.e. no byte lost nor added)

            /* NOT POSSIBLE YET mainly due to tag order and tag naming (e.g. "gnre" becoming "©gen") differences
                        FileInfo originalFileInfo = new FileInfo(location);
                        FileInfo testFileInfo = new FileInfo(testFileLocation);

                        Assert.AreEqual(originalFileInfo.Length, testFileInfo.Length);

                        string originalMD5 = TestUtils.GetFileMD5Hash(location);
                        string testMD5 = TestUtils.GetFileMD5Hash(testFileLocation);

                        Assert.IsTrue(originalMD5.Equals(testMD5));
            */
            // Get rid of the working copy
            File.Delete(testFileLocation);
        }

        [TestMethod]
        public void TagIO_RW_MP4_Unsupported_Empty()
        {
            // Source : tag-free M4A
            String testFileLocation = TestUtils.CopyAsTempTestFile(emptyFile);
            AudioDataManager theFile = new AudioDataManager(AudioData.AudioDataIOFactory.GetInstance().GetFromPath(testFileLocation));


            // Check that it is indeed tag-free
            Assert.IsTrue(theFile.ReadFromFile());

            Assert.IsNotNull(theFile.NativeTag);
            Assert.IsFalse(theFile.NativeTag.Exists);


            // Add new unsupported fields
            TagData theTag = new TagData();
            theTag.AdditionalFields.Add(new MetaFieldInfo(MetaDataIOFactory.TAG_NATIVE, "TEST", "This is a test 父"));
            theTag.AdditionalFields.Add(new MetaFieldInfo(MetaDataIOFactory.TAG_NATIVE, "TES2", "This is another test 父"));

            // Add new unsupported pictures
            PictureInfo picInfo = new PictureInfo(Commons.ImageFormat.Jpeg, MetaDataIOFactory.TAG_NATIVE, "1234");
            picInfo.PictureData = File.ReadAllBytes(TestUtils.GetResourceLocationRoot() + "_Images/pic1.jpg");
            theTag.Pictures.Add(picInfo);
            picInfo = new PictureInfo(Commons.ImageFormat.Jpeg, MetaDataIOFactory.TAG_NATIVE, "5678");
            picInfo.PictureData = File.ReadAllBytes(TestUtils.GetResourceLocationRoot() + "_Images/pic2.jpg");
            theTag.Pictures.Add(picInfo);


            theFile.UpdateTagInFile(theTag, MetaDataIOFactory.TAG_NATIVE);

            Assert.IsTrue(theFile.ReadFromFile(true, true));

            Assert.IsNotNull(theFile.NativeTag);
            Assert.IsTrue(theFile.NativeTag.Exists);

            Assert.AreEqual(2, theFile.NativeTag.AdditionalFields.Count);

            Assert.IsTrue(theFile.NativeTag.AdditionalFields.Keys.Contains("TEST"));
            Assert.AreEqual("This is a test 父", theFile.NativeTag.AdditionalFields["TEST"]);

            Assert.IsTrue(theFile.NativeTag.AdditionalFields.Keys.Contains("TES2"));
            Assert.AreEqual("This is another test 父", theFile.NativeTag.AdditionalFields["TES2"]);

            Assert.AreEqual(2, theFile.NativeTag.EmbeddedPictures.Count);
            byte found = 0;

            foreach (PictureInfo pic in theFile.NativeTag.EmbeddedPictures)
            {
                if (pic.PicType.Equals(PictureInfo.PIC_TYPE.Generic) && (0 == found)) // No custom nor categorized picture type in MP4
                {
                    Image picture = Image.FromStream(new MemoryStream(pic.PictureData));
                    Assert.AreEqual(picture.RawFormat, System.Drawing.Imaging.ImageFormat.Jpeg);
                    Assert.AreEqual(picture.Height, 600);
                    Assert.AreEqual(picture.Width, 900);
                    found++;
                }
                else if (pic.PicType.Equals(PictureInfo.PIC_TYPE.Generic) && (1 == found))  // No custom nor categorized picture type in MP4
                {
                    Image picture = Image.FromStream(new MemoryStream(pic.PictureData));
                    Assert.AreEqual(picture.RawFormat, System.Drawing.Imaging.ImageFormat.Jpeg);
                    Assert.AreEqual(picture.Height, 290);
                    Assert.AreEqual(picture.Width, 900);
                    found++;
                }
            }

            Assert.AreEqual(2, found);

            // Remove the additional unsupported field
            theTag = new TagData();
            MetaFieldInfo fieldInfo = new MetaFieldInfo(MetaDataIOFactory.TAG_NATIVE, "TEST");
            fieldInfo.MarkedForDeletion = true;
            theTag.AdditionalFields.Add(fieldInfo);

            // Remove additional picture
            picInfo = new PictureInfo(Commons.ImageFormat.Jpeg, PictureInfo.PIC_TYPE.Generic, 1);
            picInfo.MarkedForDeletion = true;
            theTag.Pictures.Add(picInfo);

            // Add the new tag and check that it has been indeed added with all the correct information
            Assert.IsTrue(theFile.UpdateTagInFile(theTag, MetaDataIOFactory.TAG_NATIVE));

            Assert.IsTrue(theFile.ReadFromFile(true, true));

            Assert.IsNotNull(theFile.NativeTag);
            Assert.IsTrue(theFile.NativeTag.Exists);

            // Additional removed field
            Assert.AreEqual(1, theFile.NativeTag.AdditionalFields.Count);
            Assert.IsTrue(theFile.NativeTag.AdditionalFields.Keys.Contains("TES2"));
            Assert.AreEqual("This is another test 父", theFile.NativeTag.AdditionalFields["TES2"]);

            // Pictures
            Assert.AreEqual(1, theFile.NativeTag.EmbeddedPictures.Count);

            found = 0;

            foreach (PictureInfo pic in theFile.NativeTag.EmbeddedPictures)
            {
                if (pic.PicType.Equals(PictureInfo.PIC_TYPE.Generic) && (0 == found))
                {
                    Image picture = Image.FromStream(new MemoryStream(pic.PictureData));
                    Assert.AreEqual(picture.RawFormat, System.Drawing.Imaging.ImageFormat.Jpeg);
                    Assert.AreEqual(picture.Height, 290);
                    Assert.AreEqual(picture.Width, 900);
                    found++;
                }
            }

            Assert.AreEqual(1, found);


            // Get rid of the working copy
            File.Delete(testFileLocation);
        }

        [TestMethod]
        public void TagIO_RW_MP4_Chapters_Nero()
        {
            ConsoleLogger log = new ConsoleLogger();

            // Source : MP3 with existing tag incl. chapters
            String testFileLocation = TestUtils.CopyAsTempTestFile("AAC/chapters_NERO.mp4");
            AudioDataManager theFile = new AudioDataManager(AudioData.AudioDataIOFactory.GetInstance().GetFromPath(testFileLocation));

            // Check if the two fields are indeed accessible
            Assert.IsTrue(theFile.ReadFromFile(false, true));
            Assert.IsNotNull(theFile.NativeTag);
            Assert.IsTrue(theFile.NativeTag.Exists);

            Assert.AreEqual(4, theFile.NativeTag.Chapters.Count);

            Dictionary<uint, ChapterInfo> expectedChaps = new Dictionary<uint, ChapterInfo>();

            ChapterInfo ch = new ChapterInfo();
            ch.StartTime = 0;
            ch.Title = "Chapter One";
            expectedChaps.Add(ch.StartTime, ch);

            ch = new ChapterInfo();
            ch.StartTime = 1139;
            ch.Title = "Chapter 2";
            expectedChaps.Add(ch.StartTime, ch);

            ch = new ChapterInfo();
            ch.StartTime = 2728;
            ch.Title = "Chapter 003";
            expectedChaps.Add(ch.StartTime, ch);

            ch = new ChapterInfo();
            ch.StartTime = 3269;
            ch.Title = "Chapter 四";
            expectedChaps.Add(ch.StartTime, ch);

            int found = 0;
            foreach (ChapterInfo chap in theFile.NativeTag.Chapters)
            {
                if (expectedChaps.ContainsKey(chap.StartTime))
                {
                    found++;
                    Assert.AreEqual(chap.StartTime, expectedChaps[chap.StartTime].StartTime);
                    Assert.AreEqual(chap.Title, expectedChaps[chap.StartTime].Title);
                }
                else
                {
                    System.Console.WriteLine(chap.StartTime);
                }
            }
            Assert.AreEqual(4, found);


            // Modify elements
            TagData theTag = new TagData();
            theTag.Chapters = new List<ChapterInfo>();
            expectedChaps.Clear();

            ch = new ChapterInfo();
            ch.StartTime = 123;
            ch.Title = "aaa";

            theTag.Chapters.Add(ch);
            expectedChaps.Add(ch.StartTime, ch);

            ch = new ChapterInfo();
            ch.StartTime = 1230;
            ch.Title = "aaa0";

            theTag.Chapters.Add(ch);
            expectedChaps.Add(ch.StartTime, ch);

            // Check if they are persisted properly
            Assert.IsTrue(theFile.UpdateTagInFile(theTag, MetaDataIOFactory.TAG_NATIVE));

            Assert.IsTrue(theFile.ReadFromFile(false, true));
            Assert.IsNotNull(theFile.NativeTag);
            Assert.IsTrue(theFile.NativeTag.Exists);

            Assert.AreEqual(2, theFile.NativeTag.Chapters.Count);

            // Check if values are the same
            found = 0;
            foreach (ChapterInfo chap in theFile.NativeTag.Chapters)
            {
                if (expectedChaps.ContainsKey(chap.StartTime))
                {
                    found++;
                    Assert.AreEqual(chap.StartTime, expectedChaps[chap.StartTime].StartTime);
                    Assert.AreEqual(chap.Title, expectedChaps[chap.StartTime].Title);
                }
                else
                {
                    System.Console.WriteLine(chap.StartTime);
                }
            }
            Assert.AreEqual(2, found);


            // Get rid of the working copy
            File.Delete(testFileLocation);
        }

        [TestMethod]
        public void TagIO_RW_MP4_Chapters_QT()
        {
            ConsoleLogger log = new ConsoleLogger();

            // Source : MP3 with existing tag incl. chapters
            String testFileLocation = TestUtils.CopyAsTempTestFile("AAC/chapters_QT.m4v");
            AudioDataManager theFile = new AudioDataManager(AudioData.AudioDataIOFactory.GetInstance().GetFromPath(testFileLocation));

            // Check if the two fields are indeed accessible
            Assert.IsTrue(theFile.ReadFromFile(false, true));
            Assert.IsNotNull(theFile.NativeTag);
            Assert.IsTrue(theFile.NativeTag.Exists);

            Assert.AreEqual(4, theFile.NativeTag.Chapters.Count);

            Dictionary<uint, ChapterInfo> expectedChaps = new Dictionary<uint, ChapterInfo>();

            ChapterInfo ch = new ChapterInfo();
            ch.StartTime = 0;
            ch.Title = "Chapter One";
            expectedChaps.Add(ch.StartTime, ch);

            ch = new ChapterInfo();
            ch.StartTime = 1139;
            ch.Title = "Chapter 2";
            expectedChaps.Add(ch.StartTime, ch);

            ch = new ChapterInfo();
            ch.StartTime = 2728;
            ch.Title = "Chapter 003";
            expectedChaps.Add(ch.StartTime, ch);

            ch = new ChapterInfo();
            ch.StartTime = 3269;
            ch.Title = "Chapter 四";
            expectedChaps.Add(ch.StartTime, ch);

            int found = 0;
            foreach (ChapterInfo chap in theFile.NativeTag.Chapters)
            {
                if (expectedChaps.ContainsKey(chap.StartTime))
                {
                    found++;
                    Assert.AreEqual(chap.StartTime, expectedChaps[chap.StartTime].StartTime);
                    Assert.AreEqual(chap.Title, expectedChaps[chap.StartTime].Title);
                }
                else
                {
                    System.Console.WriteLine(chap.StartTime);
                }
            }
            Assert.AreEqual(4, found);

            /*
                        // Modify elements -- not supported yet
                        TagData theTag = new TagData();
                        theTag.Chapters = new List<ChapterInfo>();
                        expectedChaps.Clear();

                        ch = new ChapterInfo();
                        ch.StartTime = 123;
                        ch.Title = "aaa";

                        theTag.Chapters.Add(ch);
                        expectedChaps.Add(ch.StartTime, ch);

                        ch = new ChapterInfo();
                        ch.StartTime = 1230;
                        ch.Title = "aaa0";

                        theTag.Chapters.Add(ch);
                        expectedChaps.Add(ch.StartTime, ch);

                        // Check if they are persisted properly
                        Assert.IsTrue(theFile.UpdateTagInFile(theTag, MetaDataIOFactory.TAG_NATIVE));

                        Assert.IsTrue(theFile.ReadFromFile(false, true));
                        Assert.IsNotNull(theFile.NativeTag);
                        Assert.IsTrue(theFile.NativeTag.Exists);

                        Assert.AreEqual(2, theFile.NativeTag.Chapters.Count);

                        // Check if values are the same
                        found = 0;
                        foreach (ChapterInfo chap in theFile.NativeTag.Chapters)
                        {
                            if (expectedChaps.ContainsKey(chap.StartTime))
                            {
                                found++;
                                Assert.AreEqual(chap.StartTime, expectedChaps[chap.StartTime].StartTime);
                                Assert.AreEqual(chap.Title, expectedChaps[chap.StartTime].Title);
                            }
                            else
                            {
                                System.Console.WriteLine(chap.StartTime);
                            }
                        }
                        Assert.AreEqual(2, found);
            */

            // Get rid of the working copy
            File.Delete(testFileLocation);
        }

        [TestMethod]
        public void TagIO_R_MP4_Rating()
        {
            assumeRatingInFile("_Ratings/mediaMonkey_4.1.19.1859/0.mp4", 0, MetaDataIOFactory.TAG_NATIVE);
            assumeRatingInFile("_Ratings/mediaMonkey_4.1.19.1859/0.5.mp4", 0.5 / 5, MetaDataIOFactory.TAG_NATIVE);
            assumeRatingInFile("_Ratings/mediaMonkey_4.1.19.1859/1.mp4", 1.0 / 5, MetaDataIOFactory.TAG_NATIVE);
            assumeRatingInFile("_Ratings/mediaMonkey_4.1.19.1859/1.5.mp4", 1.5 / 5, MetaDataIOFactory.TAG_NATIVE);
            assumeRatingInFile("_Ratings/mediaMonkey_4.1.19.1859/2.mp4", 2.0 / 5, MetaDataIOFactory.TAG_NATIVE);
            assumeRatingInFile("_Ratings/mediaMonkey_4.1.19.1859/2.5.mp4", 2.5 / 5, MetaDataIOFactory.TAG_NATIVE);
            assumeRatingInFile("_Ratings/mediaMonkey_4.1.19.1859/3.mp4", 3.0 / 5, MetaDataIOFactory.TAG_NATIVE);
            assumeRatingInFile("_Ratings/mediaMonkey_4.1.19.1859/3.5.mp4", 3.5 / 5, MetaDataIOFactory.TAG_NATIVE);
            assumeRatingInFile("_Ratings/mediaMonkey_4.1.19.1859/4.mp4", 4.0 / 5, MetaDataIOFactory.TAG_NATIVE);
            assumeRatingInFile("_Ratings/mediaMonkey_4.1.19.1859/4.5.mp4", 4.5 / 5, MetaDataIOFactory.TAG_NATIVE);
            assumeRatingInFile("_Ratings/mediaMonkey_4.1.19.1859/5.mp4", 1, MetaDataIOFactory.TAG_NATIVE);

            assumeRatingInFile("_Ratings/musicBee_3.1.6512/0.mp4", 0, MetaDataIOFactory.TAG_NATIVE);
            assumeRatingInFile("_Ratings/musicBee_3.1.6512/0.5.mp4", 0.5 / 5, MetaDataIOFactory.TAG_NATIVE);
            assumeRatingInFile("_Ratings/musicBee_3.1.6512/1.mp4", 1.0 / 5, MetaDataIOFactory.TAG_NATIVE);
            assumeRatingInFile("_Ratings/musicBee_3.1.6512/1.5.mp4", 1.5 / 5, MetaDataIOFactory.TAG_NATIVE);
            assumeRatingInFile("_Ratings/musicBee_3.1.6512/2.mp4", 2.0 / 5, MetaDataIOFactory.TAG_NATIVE);
            assumeRatingInFile("_Ratings/musicBee_3.1.6512/2.5.mp4", 2.5 / 5, MetaDataIOFactory.TAG_NATIVE);
            assumeRatingInFile("_Ratings/musicBee_3.1.6512/3.mp4", 3.0 / 5, MetaDataIOFactory.TAG_NATIVE);
            assumeRatingInFile("_Ratings/musicBee_3.1.6512/3.5.mp4", 3.5 / 5, MetaDataIOFactory.TAG_NATIVE);
            assumeRatingInFile("_Ratings/musicBee_3.1.6512/4.mp4", 4.0 / 5, MetaDataIOFactory.TAG_NATIVE);
            assumeRatingInFile("_Ratings/musicBee_3.1.6512/4.5.mp4", 4.5 / 5, MetaDataIOFactory.TAG_NATIVE);
            assumeRatingInFile("_Ratings/musicBee_3.1.6512/5.mp4", 1, MetaDataIOFactory.TAG_NATIVE);
        }

        [TestMethod]
        public void TagIO_RW_MP4_ID3v1()
        {
            test_RW_Cohabitation(MetaDataIOFactory.TAG_NATIVE, MetaDataIOFactory.TAG_ID3V1);
        }

        [TestMethod]
        public void TagIO_RW_MP4_ID3v2()
        {
            test_RW_Cohabitation(MetaDataIOFactory.TAG_NATIVE, MetaDataIOFactory.TAG_ID3V2);
        }

        [TestMethod]
        public void TagIO_RW_MP4_APE()
        {
            test_RW_Cohabitation(MetaDataIOFactory.TAG_NATIVE, MetaDataIOFactory.TAG_APE);
        }
    }
}
