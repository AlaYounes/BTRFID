using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fr.nexess.hao.rfid {

    /**
     * basic driving interface for rfid readers that be able to write into a tag<br/>
     * (ex : OBID I-Scan reader).
     * 
     * @version $Revision: 2 $
     * @author J.FARGEON
     * @since $Date: 2014-08-21 16:32:52 +0200 (jeu., 21 août 2014) $
     * 
     * Copyright © 2005-2014 Nexess (http://www.nexess.fr)<br/>
     * Licence: Property of Nexess
     */
    public interface TagWriter : RfidDevice {
        /**
         * Write the content of a data block into a tag.
         * 
         * @param bank Memory bank
         * @param address Starting word address in the user bank
         * @param content the byte array to put in the block. If the array is too big to fit,<br/>
         *  an Exception shall raise. If the array is too short, it shall be padded with 0.
         */
        void writeBlock(int bank, byte address, byte[] content);

        /**
         * Change the id into a tag.
         * 
         * @param tagType Tag type (Example: EPC class 1 Gen 2)
         * @param id String in hexadecimal. Ex: "E001005EF64578FFE0A84701"
         */
        void writeId(TagEnum tagType, String id);

        /**
         * Launch a request for getting the content of a tag.
         * 
         * @param bank Memory bank
         * @param address Starting word address in the user bank
         * @param num Number of 16-bits words to read
         */
        void getBlockContent(int bank, byte address, int num);
    }

    public enum TagEnum {
        PHILIPS_I_CODE1 = 0x00,          // Philips I-CODE1
        TEXAS_INSTRUMENTS_TAG_IT_HF = 0x01,// Texas Instruments Tag-it HF
        ISO15693 = 0x03,                 // ISO15693
        ISO14443A = 0x04,                // ISO14443A
        ISO14443B = 0x05,                // ISO14443B
        I_CODE_UID = 0x07,               // I-Code UID
        INNOVISION_JEWEL = 0x08,         // Innovision Jewel
        ISO_18000_3M3 = 0x09,            // ISO 18000-3M3
        ISO18000_6_B = 0x81,             // ISO18000-6-B
        EM4222 = 0x83,                   // EM4222
        EPC_CLASS1_GEN2 = 0x84,          // EPC Class1 Gen2
        EPC_CLASS0_0PLUS = 0x88,         // EPC Class0/0+
        EPC_CLASS1_GEN1 = 0x89,          // EPC Class1 Gen1
        EPC = 0x06                       // EPC (Electronic Product Code)  
    }
}
