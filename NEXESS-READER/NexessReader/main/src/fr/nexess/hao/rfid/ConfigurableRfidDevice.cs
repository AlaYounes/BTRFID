using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fr.nexess.hao.rfid {

    public enum ConfigurableReaderParameter {
        READER_NAME,
        TAG_SCAN_DURATION,
        RSSI_ENABLED,
        CLOSEST_SCAN_ENABLED,
        COM_PORT,
        CONNECTION_TYPE,
        NB_MAX_TAGS,
        TIMEOUT_CONNECT,
        NO_DISPATCHER_QUEUE,
        SETTINGS_FILE
    }
    /**
     * interface for readers that must provide a configuration service.
     *
     * @version $Revision: 162 $
     * @author J.FARGEON
     * @since $Date: 2016-08-08 16:16:23 +0200 (lun., 08 août 2016) $
     * 
     * Copyright © 2005-2014 Nexess (http://www.nexess.fr)<br/>
     * Licence: Property of Nexess
     */
    public interface ConfigurableRfidDevice {
        /**
        * Set a Map of key-value couples of parameter. This is highly device-dependent and what exactly will be
        * the available keys is left to the coder.
        */
        void setParameter(String key, Object value);

        /**
        * Get a Map of key-value couples of parameter. This is highly device-dependent and what exactly will be
        * the available keys is left to the coder.
        */
        Dictionary<String, Object> getParameters();

        /**
         * Get Power of an antenna
         */
        float getPower(int antenna);

        /**
         * Set Power to an antenna
         */
        void setPower(int antenna, float power);

        void setPowerDynamically(int antenna, float power);

        ///**
        // * Get  antenna configuration (activated antenna)
        // */
        byte getAntennaConfiguration();

        ///**
        // * Set  antenna configuration (activated antenna)
        // */
        //void setAntennaConfiguration(byte configAntenna);

        ///**
        // * Add  specified antenna to the configuration
        // */
        //byte addAntenna(int antenna);
        ///**
        // * Remove  specified antenna from the configuration
        // */
        //byte removeAntenna(int antenna);

        ///**
        // * Get  Multiplexer status
        // */
        //bool isMuxEnabled(int port);

        ///**
        // * Read ports and detect if a multiplexer is connected
        // */
        //void detectMux(int port);

        ///**
        // * Get  mux configuration (activated antenna)
        // */
        //byte getMuxConfiguration(int port);

        ///**
        // * Set  mux configuration (activated antenna)
        // */
        //void setMuxConfiguration(byte configMux, int port);

        ///**
        // * Add  specified antenna to the mux configuration
        // */
        //byte addMuxAntenna(int readerPort, int muxPort);

        ///**
        // * Remove  specified antenna to the mux configuration
        // */
        //byte removeMuxAntenna(int readerPort, int muxPort);

        ///**
        // * Disable multiplexer
        // */
        //byte removeMux(int antenna);
    }
}
