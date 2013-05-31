/**************************************************************************************************
 * Constants.cs
 * 
 * Copyright 1998-2003 XiaoCui' Technology Co.,Ltd.
 * 
 * DESCRIPTION: - Command
 * 	   
 * modification history
 * --------------------
 * 01a, 02.25.2013, cuiqingwei written
 * --------------------
 **************************************************************************************************/
using System;
using System.Collections.Generic;
using System.Text;

namespace CC2530ZNP
{
    public class Constants
    {
        /* Device Type */
        public enum LogicType
        {
            Coodinator,            
            Router,
            EndDevice
        };
        /* Command */
        // SYS interface
        public const ushort SYS_RESET_REQ                   = 0x4100;
        public const ushort SYS_RESET_IND                   = 0x4180;
        public const ushort SYS_VERSION                     = 0x2102;
        public const ushort SYS_VERSION_SRSP                = 0x6102;
        // ZDO interface
        public const ushort ZDO_STARTUP_FROM_APP            = 0x2540;
        public const ushort ZDO_STARTUP_FROM_APP_SRSP       = 0x6540;
        // Configuration interface 
        public const ushort ZB_START_REQUEST                = 0x2600;
        public const ushort ZB_START_REQUEST_SRSP           = 0x6600;
        public const ushort ZB_SEND_DATA_REQUEST            = 0x2603;
        public const ushort ZB_SEND_DATA_REQUEST_SRSP       = 0x6603;
        public const ushort ZB_READ_CONFIGURATION           = 0x2604;
        public const ushort ZB_READ_CONFIGURATION_SRSP      = 0x6604;
        public const ushort ZB_WRITE_CONFIGURATION          = 0x2605;
        public const ushort ZB_WRITE_CONFIGURATION_SRSP     = 0x6605;
        // Simple API 
        public const ushort ZB_APP_REGISTER_REQUEST         = 0x260A;
        public const ushort ZB_APP_REGISTER_REQUEST_SRSP    = 0x660A;
        // Util interface    
        public const ushort UTIL_GET_DEVICE_INFO            = 0x2700;
        public const ushort UTIL_GET_DEVICE_INFO_SRSP       = 0x6700;
        // 
        public const ushort APP_MSG                         = 0x2900;
        //
        public const ushort ZB_SEND_DATA_CONFIRM            = 0x4683;
        public const ushort ZB_RECEIVE_DATA_INDICATION      = 0x4687;
        /* Config Param */
        // Configuration ID: 0x0083; Size: 2 bytes; Default value: 0xFFFF 
        public const ushort ZCD_NV_PAN_ID                   = 0x0083;
        // Configuration ID: 0x0084; Size: 4 bytes; Default value: 0x00000800 
        public const ushort ZCD_NV_CHANLIST                 = 0x0084;
        // Configuration ID: 0x0087; Size: 1 byte; Default value: 0x00 
        public const ushort ZCD_NV_LOGICAL_TYPE             = 0x0087;
    }
}

/*-------------------------------------------------------------------------------------------------
								             	 0ooo
							           	ooo0     (   )
								        (   )     ) /
							           	 \ (     (_/
	    				                  \_)        By:cuiqingwei [gary]
--------------------------------------------------------------------------------------------------*/