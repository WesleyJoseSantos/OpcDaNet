//============================================================================
// DANSrv Customization          OPC DA Server
// --------------------
//
// All items are defined at startup.
// No custom item properties are defined and handled.
// Refresh requests from the generic server are not handled because all item values
// are written into the cache as soon as they are changed. The cache is always up-to-date.
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
//
// Copyright (C) 2003-2015 Advosol Inc.    (www.advosol.com)
// All rights reserved.
//-------------------------------------------------------------------------
using System;
using System.Threading;
using System.Configuration;
using System.IO;

namespace NSPlugin
{

    //===============================================================================
    // OPC Server Configuration and IO Handling
    //===============================================================================
    public class AppPlugin : GenericServer
    {

        //-----------------------------------------------------------------
        /// <summary>
        /// This method is called from the generic server EXE at startup.
        /// It creates all items supported by the OPC server.
        /// This method needs to call the callback method "AddItem" to add the item to the server's address space.
        /// The Item IDs are fully qualified names ( eg. Dev1.Chn5.Temp ).
        /// The generic server part creates an appropriate hierarchical address space.
        /// The sample code defines the application item handle as the the buffer array index.
        /// This handle is passed in the calls from the generic server to identify the item. 
        /// It should allow quick access to the signal state buffer.
        /// The handle may be implemented differently depending on the application.
        /// </summary>
        /// <param name="pszParamas">[in] String as defined in the server command-line 
        /// during the server registration.
        /// OPCappsrv /RegServer /PARAM:string </param>
        /// <returns>HRESULTS error/success Code</returns>
        new public int CreateServerItems(string cmdParams)
        {
            int rtc = HRESULTS.S_OK;
            //------------------------ create all items from the definition table
            for (int i = 0; i < Config.Items.Length; ++i)
            {
                AddItem(i, Config.Items[i].name, (int)OPCAccess.READWRITEABLE, Items[i].Value,
                   (short)OPCQuality.GOOD, DateTime.UtcNow, 500);
            }

            if (HRESULTS.Succeeded(rtc))
            {
                //------------------------ create a thread for simulating signal changes
                // in real application this thread reads from the device
                myThread = new Thread(new ThreadStart(RefreshThread));
                myThread.Name = "Item Simulation";
                myThread.Start();
            }
            return HRESULTS.S_OK;
        }

        //----------------------------------------------------------------
        /// <summary>
        /// This method is called from the generic server when a shutdown is executed 
        /// because all clients have disconnected.
        /// To ensure proper process shutdown, all used communication channels should 
        /// be closed and threads terminated before this method returns.
        /// </summary>
        new public void ShutdownSignal()
        {
            ////////////////// TO-DO /////////////////
            // close the device communication

            // stop the simulation thread
            StopThread = new ManualResetEvent(false);
            StopThread.WaitOne(5000, true);
            StopThread.Close();
            StopThread = null;
        }

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// This method is called when a client executes a 'write' server call. 
        /// The items specified in the appHandles array need to be written to the device. 
        /// The generic server updates the cache for these items.
        /// </summary>
        /// <param name="instanceHandle">Handle the identifies the calling client application. 
        /// The method GetServerInstancesInfo can be used to get name information for this handle.</param>
        /// <param name="values">Array of objects with handle, value, quality, timestamp</param>
        /// <param name="errors">Array with S_OK or OPC HRESULT error codes on return.</param>
        /// <returns>S_OK or OPC HRESULT error code</returns>
        new public int WriteItems(int instanceHandle, DeviceItemValue[] values, out int[] errors)
        {
            errors = new int[values.Length];           // result array
            for (int i = 0; i < values.Length; ++i)       // init to S_OK
                errors[i] = HRESULTS.S_OK;

            // write the new values to the device
            for (int i = 0; i < values.Length; ++i)       // handle all items
            {
                int ItemHandle = values[i].Handle;
                Config.Items[ItemHandle].Value = values[i].Value;   // write value into buffer
            }
            return HRESULTS.S_OK;
        }

        //-------------------------------------------------------------------------------------
        /// <summary>
        /// This method overload is called from the Standard Edition generic server.
        /// </summary>
        /// <param name="values">object with handle, value, quality, timestamp</param>
        /// <param name="errors">array with S_OK or error codes on return.</param>
        /// <returns></returns>
        new public int WriteItems(DeviceItemValue[] values, out int[] errors)
        {
            return WriteItems(0, values, out errors);
        }

        //----------------------------------------------------------------------------
        // DATA DEFINITIONS
        // Important: All data needs to be defined as STATIC.
        // This is important because this class is used in multiple instances.
        //----------------------------------------------------------------------------
        // Driver internal signal state buffer. The Item Handle is the array index.
        static ItemDef[] Items = {  new ItemDef("TEST",   (int)1111 )  };
        static internal ConfigData Config = new ConfigData(Items);
        static Thread myThread;
        static ManualResetEvent StopThread = null;

        //=======================================================================================
        // Simulate item value changes and write the changed values to the generic server's cache
        void RefreshThread()
        {
            for (; ; )     // forever thread loop
            {
                // increment readable items of type Int, Short, Float or Double
                for (int i = 0; i < Config.Items.Length; ++i)
                {
                    System.Type tp = Config.Items[i].Value.GetType();
                    if (tp == typeof(int))
                    {
                        int v = (int)Config.Items[i].Value;
                        Config.Items[i].Value = v + 1;
                    }
                    SetItemValue(i, Config.Items[i].Value, (short)OPCQuality.GOOD, DateTime.UtcNow);
                }

                Thread.Sleep(500);      // ms

                if (StopThread != null)
                {
                    StopThread.Set();
                    return;                 // terminate the thread
                }
            }
        }
    }
}
