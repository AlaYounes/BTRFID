using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using fr.nexess.hao.rfid.device.axesstmc;

namespace NexessRfidDeviceTest.main.src.fr.nexess.hao.rfid.device.axesstmc {
    [TestClass]
    public class LegicFrameBuilderTest {

        [TestMethod]
        public void should_build_frame_GET_READER_INFO() {

            // arrange / act
            byte[] expectedFrame = new byte[] { 0x05, 0xB6, 0x00, 0x01, 0x01, 0xB3 };

            byte[] frame = LegicFrameRebuilder.buildFrame(  LegicProtocol.CMD.GET_READER_INFO_0xB6.GET_READER_INFO, 
                                                            LegicProtocol.CMD.GET_READER_INFO_0xB6.PARAMS);

            // assert
            Assert.IsTrue(expectedFrame.Length == frame.Length);
            CollectionAssert.AreEqual(expectedFrame, frame);
        }

        [TestMethod]
        public void should_build_frame_COMMAND_MODE() {

            // arrange / act
            byte[] expectedFrame = new byte[] { 0x03, 0x14, 0x00, 0x17};

            byte[] frame = LegicFrameRebuilder.buildFrame(LegicProtocol.CMD.MODE_0x14.MODE, LegicProtocol.CMD.MODE_0x14.COMMAND_MODE);

            // assert
            Assert.IsTrue(expectedFrame.Length == frame.Length);
            CollectionAssert.AreEqual(expectedFrame, frame);
        }

        [TestMethod]
        public void should_build_frame_AUTO_READING_MODE() {

            // arrange / act
            byte[] expectedFrame = new byte[] { 0x03, 0x14, 0x01, 0x16 };

            byte[] frame = LegicFrameRebuilder.buildFrame(LegicProtocol.CMD.MODE_0x14.MODE, LegicProtocol.CMD.MODE_0x14.AUTOMATIC_READING);

            // assert
            Assert.IsTrue(expectedFrame.Length == frame.Length);
            CollectionAssert.AreEqual(expectedFrame, frame);
        }

        [TestMethod]
        public void should_rebuild_frame_and_raise_TagDecoded_event() { 

            // arrange
            String expectedValue = "0000000041B9B8AB";
            List<String> frames = new List<String>() { "0D790000080000000041B9B8AB97" };

            LegicFrameRebuilder frameRebuilder = new LegicFrameRebuilder();

            String snr  = "";
            frameRebuilder.TagDecoded += (object sender, TagDecodedEventArgs e) => snr = e.Snr;

            // act
            frameRebuilder.rebuildFrames(frames);


            // assert
            Assert.IsTrue(!String.IsNullOrEmpty(snr));
            Assert.IsTrue(String.Compare(expectedValue,snr)==0);
        }

        [TestMethod]
        public void should_rebuild_double_frame_and_raise_2_TagDecoded_events() {

            // arrange
            List<String> frames = new List<String>() { "0D790000080000000041B9B8AB97", "0D790000080000000041B9B8AB97" };

            LegicFrameRebuilder frameRebuilder = new LegicFrameRebuilder();

            int count = 0;
            frameRebuilder.TagDecoded += (object sender, TagDecodedEventArgs e) => count++;

            // act
            frameRebuilder.rebuildFrames(frames);

            // assert
            Assert.IsTrue(count == 2);
        }

        [TestMethod]
        public void should_rebuild_frame_with_crappy_bytes_and_raise_TagDecoded_events() {

            // arrange
            String expectedValue = "0000000041B9B8AB";
            List<String> frames = new List<String>() { "0F060D790000080000000041B9B8AB97" };// start with crappy 0x0F 0x06 bytes

            LegicFrameRebuilder frameRebuilder = new LegicFrameRebuilder();

            String snr  = "";
            frameRebuilder.TagDecoded += (object sender, TagDecodedEventArgs e) => snr = e.Snr;

            // act
            frameRebuilder.rebuildFrames(frames);

            // assert
            Assert.IsTrue(!String.IsNullOrEmpty(snr));
            Assert.IsTrue(String.Compare(expectedValue, snr) == 0);
        }

        [TestMethod]
        public void should_not_rebuild_frame_and_raise_no_TagDecoded_event() {

            // arrange
            List<String> frames = new List<String>() { "0D790000080000000041B9B8AB96" };

            LegicFrameRebuilder frameRebuilder = new LegicFrameRebuilder();

            String snr  = "";
            frameRebuilder.TagDecoded += (object sender, TagDecodedEventArgs e) => snr = e.Snr;

            // act
            frameRebuilder.rebuildFrames(frames);

            // assert
            Assert.IsTrue(String.IsNullOrEmpty(snr));

        }

        [TestMethod]
        public void should_rebuild_splitted_frame_and_raise_TagDecoded_event() {

            // arrange
            String expectedValue = "0000000041B9B8AB";
            List<String> frames = new List<String>() { "0D790000080000000041B9B8AB", "97" };

            LegicFrameRebuilder frameRebuilder = new LegicFrameRebuilder();

            String snr  = "";
            frameRebuilder.TagDecoded += (object sender, TagDecodedEventArgs e) => snr = e.Snr;

            // act
            frameRebuilder.rebuildFrames(frames);


            // assert
            Assert.IsTrue(!String.IsNullOrEmpty(snr));
            Assert.IsTrue(String.Compare(expectedValue, snr) == 0);
        }

        [TestMethod]
        public void should_rebuild_frame_in_several_time_and_raise_TagDecoded_event() {

            // arrange
            String expectedValue = "0000000041B9B8AB";
            List<String> frames1 = new List<String>() { "0D790000080000000041B9B8AB" };
            List<String> frames2 = new List<String>() { "97" };

            LegicFrameRebuilder frameRebuilder = new LegicFrameRebuilder();

            String snr  = "";
            frameRebuilder.TagDecoded += (object sender, TagDecodedEventArgs e) => snr = e.Snr;

            // act
            frameRebuilder.rebuildFrames(frames1);
            frameRebuilder.rebuildFrames(frames2);

            // assert
            Assert.IsTrue(!String.IsNullOrEmpty(snr));
            Assert.IsTrue(String.Compare(expectedValue, snr) == 0);
        }

        [TestMethod]
        public void should_rebuild_frame_in_4_times_and_raise_TagDecoded_event() {

            // arrange
            String expectedValue = "0000000041B9B8AB";

            List<String> frames1 = new List<String>() { "0D7900" };
            List<String> frames2 = new List<String>() { "00" };
            List<String> frames3 = new List<String>() { "080000000041B9B8AB" };
            List<String> frames4 = new List<String>() { "97" };

            LegicFrameRebuilder frameRebuilder = new LegicFrameRebuilder();

            String snr  = "";
            frameRebuilder.TagDecoded += (object sender, TagDecodedEventArgs e) => snr = e.Snr;

            // act
            frameRebuilder.rebuildFrames(frames1);
            frameRebuilder.rebuildFrames(frames2);
            frameRebuilder.rebuildFrames(frames3);
            frameRebuilder.rebuildFrames(frames4);

            // assert
            Assert.IsTrue(!String.IsNullOrEmpty(snr));
            Assert.IsTrue(String.Compare(expectedValue, snr) == 0);
        }

        [TestMethod]
        public void should_not_rebuild_frame_in_too_many_time_and_raise_no_TagDecoded_event() {

            // arrange
            List<String> frames1 = new List<String>() { "0D7900" };
            List<String> frames2 = new List<String>() { "00" };
            List<String> frames3 = new List<String>() { "080000000041B9B8" };
            List<String> frames4 = new List<String>() { "AB" };
            List<String> frames5 = new List<String>() { "97" };

            LegicFrameRebuilder frameRebuilder = new LegicFrameRebuilder();

            String snr  = "";
            frameRebuilder.TagDecoded += (object sender, TagDecodedEventArgs e) => snr = e.Snr;

            // act
            frameRebuilder.rebuildFrames(frames1);
            frameRebuilder.rebuildFrames(frames2);
            frameRebuilder.rebuildFrames(frames3);
            frameRebuilder.rebuildFrames(frames4);
            frameRebuilder.rebuildFrames(frames5);

            // assert
            Assert.IsTrue(String.IsNullOrEmpty(snr));
        }

        [TestMethod]
        public void should_rebuild_the_second_splitted_frame_and_raise_TagDecoded_event() {

            // arrange
            String expectedValue = "0000000041B9B8AB";

            List<String> frames1 = new List<String>() { "0D7900" };
            List<String> frames2 = new List<String>() { "00" };
            List<String> frames3 = new List<String>() { "080000000041B9B8" };
            List<String> frames4 = new List<String>() { "AB97" };

            List<String> frames1b = new List<String>() { "0D7900" };
            List<String> frames2b = new List<String>() { "00" };
            List<String> frames3b = new List<String>() { "080000000041B9B8AB97" };

            LegicFrameRebuilder frameRebuilder = new LegicFrameRebuilder();

            String snr  = "";
            frameRebuilder.TagDecoded += (object sender, TagDecodedEventArgs e) => snr = e.Snr;

            // act
            frameRebuilder.rebuildFrames(frames1);
            frameRebuilder.rebuildFrames(frames2);
            frameRebuilder.rebuildFrames(frames3);
            frameRebuilder.rebuildFrames(frames4);

            frameRebuilder.rebuildFrames(frames1b);
            frameRebuilder.rebuildFrames(frames2b);
            frameRebuilder.rebuildFrames(frames3b);

            // assert
            Assert.IsTrue(!String.IsNullOrEmpty(snr));
            Assert.IsTrue(String.Compare(expectedValue, snr) == 0);
        }
    }
}
