//=====================================================================================
// DANSrvAE OPC DA/AE .NET customizable Server       Customization Plugin .Net Assembly
// -------------------------------------------
//
// C# customization plugin with .NET interface to the generic OPC server part.
//
// This module contains default implementations for the DA servers calls from the generic server
// and provides wrapper methods for callbacks to the generic DA server.
// Most of the DA call handling methods are overloaded with a custom implementation
// in the ServerAdapt.cs module.
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
//
// Copyright (C) 2003-2010 Advosol Inc.    (www.advosol.com)
// All rights reserved.
// ---------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices;
using System.Threading ;
using System.Configuration;
using System.IO;

namespace NSPlugin
{


   /// <summary>
   /// Server information structure returned by GetServerRegistryDef.
   /// </summary>
   public struct SrvRegDef 
   {     
      /// <summary>
      /// The DANSrv DCOM server is registered with this CLSID.
      /// </summary>
      public string ClsidServer ;
      /// <summary>
      ///  The DANSrv DCOM server application is registered with this CLSID in the AppID section.
      /// </summary>
      public string ClsidApp ;
      /// <summary>
      /// Version independent ProgID of the DANSrv DCOM server.
      /// </summary>
      public string PrgidServer ;
      /// <summary>
      /// ProgID of the current DANSrv DCOM server version.
      /// </summary>
      public string PrgidCurrServer ;
      /// <summary>
      /// Version independent friendly name of the DANSrv DCOM server.
      /// </summary>
      public string NameServer ;
      /// <summary>
      /// Version independent friendly name of the current DANSrv DCOM server version. 
      /// The XDARap XML DA server returns this definition in the client GetStatus call.
      /// </summary>
      public string NameCurrServer ;
      /// <summary>
      /// Server vendor name. Used by DANSrv and XDARap.
      /// </summary>
      public string CompanyName ;
   };

   /// <summary>
   /// Item data passed in WriteItems()
   /// </summary>
   public class DeviceItemValue
   {
      /// <summary>
      /// Application handle of the item.<br/>
      /// This handle is used between the generic server and the customization module to
      /// identify the items. The customization module should define the habndel so that it 
      /// allows quick access to the data structures needed in handing the requests.
      /// </summary>
      public int        Handle ;
      /// <summary>
      /// Value to be written to the device.<br/>
      /// The item value must alway be written to the generic server cache in the canonical 
      /// data type defined when the item was added.
      /// </summary>
      public object     Value ;
      /// <summary>
      /// Item quality.<br/>
      /// The quality value is according the OPC DA V2 specifications.
      /// </summary>
      public short      Quality ;
      /// <summary>
      /// Indicates that the item quality is specified.
      /// </summary>
      public bool       QualitySpecified ;
      /// <summary>
      /// Timestamp for this item value.
      /// </summary>
      public DateTime   Timestamp ;
      /// <summary>
      /// Indicates that the timestamp is specified.
      /// </summary>
      public bool       TimestampSpecified ;
   }

   /// <summary>
   /// Node info returned in browse
   /// </summary>
   public class BrowseNodeInfo
   {
      /// <summary>
      /// Short user friendly portion of the namespace pointing to the element. 
      /// This is the string to be used for display purposes in a tree control.
      /// </summary>
      public string   Name;
      /// <summary>
      /// The unique identifier for this item.that can be used with AddItems, 
      /// Browse or GetProperties.
      /// </summary>
      public string   ItemID; 
      /// <summary>
      /// The current bits available to be set are: 
      /// OPC_BROWSE_HASCHILDREN = 0x01 
      /// OPC_BROWSE_ISITEM = 0x02 
      /// If the first bit is set (OPC_BROWSE_HASCHILDREN), then this indicates that the 
      /// returned element has children and can be used for a subsequent browse. 
      /// If it is too time consuming for a server to determine if an element has children, 
      /// then this value should be set TRUE so that the the client is given the opportunity 
      /// to attempt to browse for potential children. If the second bit is set (OPC_BROWSE_ISITEM) 
      /// then the element is an item that can be used to Read, Write, and Subscribe. 
      /// If the second bit is set and szItemID is a NULL string, then this element is a �hint� 
      /// versus being a valid item.
      /// </summary>
      public int      Flags;
   }



   /// <summary>
   /// Item info from the generic server cache
   /// </summary>
   public class CacheItemInfo
   {
      /// <summary>
      /// Number of group items currently linked to this device item.
      /// </summary>
      public int   GroupLinkCounter ;  
      /// <summary>
      /// Number of active group items currently linked to this device item.
      /// </summary>
      public int   GroupActiveLinkCounter ;
      /// <summary>
      /// Customization plug-in defined item handle. This handle is used to identify the item 
      /// in the data exchange between the generic server and the plug-in.
      /// </summary>
      public int   AppHandle ;  
      /// <summary>
      /// Plug-in defined sampling rate for this item. This definiton is used only when no client 
      /// did define a sampling rate for the item.
      /// </summary>
      public int   ScanRate; 
      /// <summary>
      /// Client defined sampling rate. If defined, then this item is refreshed with this rate.
      /// The value -1 stands for 'undefined'.
      /// </summary>
      public int   SamplingRate; 
      /// <summary>
      /// Canonical data type as a VARIANT VT_.. enumeration value.
      /// </summary>
      public Type  DataType; 
   }



   /// <summary>
   /// Information from the generic server about the current server instances.
   /// There is an instance for each client connection. 
   /// The application may use this information to give some clients special access rights
   /// or restrict the access for some clients. E.g. deny write access or read/write access to certain items.
   /// Since the always available user account information may be the same for all clients, the client
   /// application neeed to set define a name using the SetClientName OPC function.
   /// </summary>
   [StructLayout(LayoutKind.Sequential, Pack=0, CharSet=CharSet.Unicode)]
   public class ServerInstanceInfo
   {
      /// <summary>
      /// Generic server internal handle for the instance.
      /// This handle is used in some NSPlugin interface methods to identify the server instance.
      /// The server may implement client application dependent handling.
      /// </summary>
      [MarshalAs(UnmanagedType.I4)]
      public int     handle ;

      /// <summary>
      /// Number of groups the client application has currently added.
      /// </summary>
      [MarshalAs(UnmanagedType.I4)]
      public int numberOfGroups;

      /// <summary>
      /// The name of the client as set by the client in the method SetClientName.
      /// The name is an empty string by default.
      /// </summary>
      [MarshalAs(UnmanagedType.LPWStr)]
      public string  clientName;

      /// <summary>
      /// The Windows account name running the server instance. 
      /// Depending on the server DCOM configuration this may be the client application account 
      /// or the user account defined in the DCOM configuration, Identity tab.
      /// </summary>
      [MarshalAs(UnmanagedType.LPWStr)]
      public string clientAccount;
   }


   //======================================================================
   // These enumerations match the OPC specifications or the server EXE file.
   // -----------

   /// <summary>
   /// Possible OPC server states
   /// </summary>
   public enum OpcServerState
   {
      /// <summary>
      /// Normal running mode state. This state is set after the CreateServerItems method
      /// is executed.
      /// </summary>
      OPC_STATUS_RUNNING	   = 1,
      /// <summary>
      /// State set by the customization plug-in to indicate a general error
      /// situation.
      /// </summary>
      OPC_STATUS_FAILED	      = 2,
      /// <summary>
      /// At startup the server start is in this state until CreateServerItems is
      /// executed.
      /// </summary>
      OPC_STATUS_NOCONFIG	   = 3,
      /// <summary>
      /// State set by the customization plug-in to indicate that the normal server
      /// function is currently suspended.
      /// </summary>
      OPC_STATUS_SUSPENDED	   = 4,
      /// <summary>
      /// State set by the customization plug-in to indicate that the serve is currently in
      /// test mode.
      /// </summary>
      OPC_STATUS_TEST	      = 5,
      /// <summary>
      /// State set by the customization plug-in to indicate that the communication to the
      /// device is currently not working.
      /// </summary>
      OPC_STATUS_COMM_FAULT	= 6
   }

   /// <summary>
   /// OPC defined item read/write definitions
   /// </summary>
   public enum OPCAccess : short 
   {
      /// <summary>Item is read only</summary>
      READABLE       = 1,
      /// <summary>Item is write only</summary>
      WRITEABLE      = 2,
      /// <summary>Item is read/writable</summary>
      READWRITEABLE  = 3
   }

   /// <summary>
   /// Controls the client update handling in the generic server.
   /// Professinal Edition only. The Standard Edition only supports the 'ManyChanges' mode.
   /// </summary>
   public enum ClientUpdateHandlingMode
   {
      /// <summary>
      /// (0)  In this mode the update handling is optimized for troughput (many value changes).
      /// The client update thread periodically checks the cache for all items in active groups.
      /// This handling is optimal if there are up to hundreds of items in the client groups and
      /// there are rather many value changes.
      /// </summary>
      ManyChanges = 0,
      /// <summary>
      /// (1)  In this mode the update handling is optimized for client groups with a large number of items.
      /// The cache update handler writes changes items in a queue for each client group.
      /// The client update thread transfers the values of the queued items to the client.
      /// This mode is optimal if there are a large number of items in the client groups but 
      /// there are typically less than 1000 value changes per second.
      /// </summary>
      ManyItems = 1,
   }


   /// <summary>
   /// Determines how the server starts when the EXE file is started.<br/>
   /// This definition has no effect if the server is started from DCOM due to a client connect
   /// or when it's registered to run as a service.
   /// The server terminates after the last client disconnects, independent of this definition. 
   /// </summary>
   public enum ExeStartMode
   {
      /// <summary>
      /// (0)  The generic server initalizes but doesn't call the plugin initialization methods 
      /// GetServerParameters and CreateServerItems before the first client connects.
      /// The startup behavior allows e.g. a debugger to be attached before the normal server 
      /// operation starts.
      /// </summary>
      Partial = 0,
      /// <summary>
      /// (1)  The server starts completely up without waiting for a client to connect. 
      /// The server customization can be debugged without a client having to connect.
      /// </summary>
      Full = 1
   }



   /// <summary>
   /// Added in DANSrv V6.0<br/>
   /// Determines how the LookupItgemIDs client requests are handled and how property items are created and updated.
   /// </summary>
   public enum LookupMode
   {
      /// <summary>
      /// LookupItemIDs requests are handled in the generic server. NSPlugin.dll is not involved.
      /// The generic server creates a thread that calls every 2 seconds the method GetPropertyValue() to update the 
      /// value of the property item in teh generic server cache.
      /// </summary>
      Generic = 0, 
      /// <summary>
      /// Behaves as mode "Intern" but doesn't start the property value update thread. 
      /// Servers with only constant property values can prevent the update thread overhead.
      /// </summary>
      GenericNoUpdate=1, 
      /// <summary>
      /// Property items are handled much like regular items. 
      /// On LookupItemIDs client requests the generic server call the plug-in method LookupItemID().
      /// This method creates the property item by calling the generic server method AddItem().
      /// The plug-in must update the value of this item by calling the SetItemValue() method.
      /// </summary>
      PluginHandled=2,
   }


   /// <summary>
   /// Controls the cache update in the Write handling.
   /// Professinal Edition only. The Standard Edition only supports the GenericServer mode.
   /// </summary>
   public enum WriteCacheUpdateHandlingMode
   {
      /// <summary>
      /// (0)  The cache is updated in the generic server after returning from the customization
      /// WiteItems method. Items with write error are not updated in the cache.
      /// </summary>
      GenericServer = 0,
      /// <summary>
      /// (1)  The generic server does NOT update the cache. The customization module has to update
      /// the cache by executing the SetItemValue callback method for each written item.
      /// </summary>
      Custom = 1,
   }

   
   /// <summary>
   /// Item Validate mode enumerator
   /// </summary>
   public enum ValidateMode
   {
      /// <summary>
      /// (0)  The plug-in ValidateItems method is called for items that are not found in the generic server cache.
      /// </summary>
      UnknownItems = 0,
      /// <summary>
      /// (1)  The plug-in ValidateItems method is NEVER called.
      /// </summary>
      Never = 1,
      /// <summary>
      /// (2)  The plug-in ValidateItems method is ALWAYS called. 
      /// </summary>
      Always = 2
   }


   /// <summary>
   /// Item Validate mode enumerator
   /// </summary>
   public enum ValidateReason
   {
      /// <summary>
      /// (0)  Client calls OPC DA V2 ValidateItems
      /// </summary>
      ValidateItems = 0,
      /// <summary>
      /// (1)  Client calls OPC DA V2 AddItems resp. Subscribe in XML DA server
      /// </summary>
      AddItems = 1,
      /// <summary>
      /// (2)  Client call OPC DA V3 ItemIO Read
      /// </summary>
      Read = 2,
      /// <summary>
      /// (3)  Client call OPC DA V3 ItemIO WriteVQT
      /// </summary>
      Write = 3,
      /// <summary>
      /// (4)  Client call OPC DA V2 ItemIO QueryAvailableProperties
      /// </summary>
      QueryAvailableProperties = 4,
      /// <summary>
      /// (5)  Client call OPC DA V2 or XML DA GetItemProperties
      /// </summary>
      GetItemProperties = 5,
      /// <summary>
      /// (6)  Client call OPC DA V3 GetProperties
      /// </summary>
      GetPropertiesV3 = 6,
      /// <summary>
      /// (6)  Client call LookupItemIDs
      /// </summary>
      LookupItemIDs = 7,
   }




   /// <summary>
   /// Browse Mode enumerator
   /// </summary>
   public enum BROWSEMODE
   {
      /// <summary>
      /// (0)  Browse calls are handled in the generic server and return the item/branches that
      /// are defined in the cache.
      /// </summary>
      REAL		   = 0,
      /// <summary>
      /// (1)  Browse calls are handled in the plug-in and typically return all items that could
      /// be dynamically added to the cache.
      /// </summary>
      VIRTUAL		= 2
   }

   /// <summary>
   /// Alarms/Events Browse type definition for filters.
   /// Event sources are organized in a hierarchical structure of areas. Areas can be defined 
   /// as required and must not correlate to branches in the DA server address space.<br/>
   /// However, for simplicity reasons the sample implementation has the eventsorces in areas that 
   /// correlate to the DA server branch structure.
   /// </summary>
   public enum EventBrowseType
   {
      /// <summary>
      /// (1)  The filter criteria is applied to areas.
      /// </summary>
      Area	= 1,
      /// <summary>
      /// (2)  The filter criteria is applied to event sources.
      /// </summary>
      Source	= 2
   }


   /// <summary>
   /// Browse direction enumerator used in the OPC DA browser and The OPC AE area browser.
   /// </summary>
   public enum OPCBROWSEDIRECTION
   {
      OPC_BROWSE_UP		= 1,
      OPC_BROWSE_DOWN	= 2,
      OPC_BROWSE_TO		= 3
   }
   /// <summary>
   /// Enumerator for for browse mode selction
   /// </summary>
   public enum OPCBROWSETYPE
   {
      OPC_BRANCH	= 1,
      OPC_LEAF	   = 2,
      OPC_FLAT	   = 3
   }

   /// <summary>
   /// OPC DA V3 browse modes
   /// </summary>
   public enum OPCV3BrowseType
   {
      OPC_BROWSE_FILTER_ALL	   = 1,
      OPC_BROWSE_FILTER_BRANCHES	= 2,
      OPC_BROWSE_FILTER_ITEMS	   = 3
   }

   /// <summary>
   /// OPC DA V3 browse node information
   /// </summary>
   public enum OPCV3NodeType
   {
      OPC_BROWSE_HASCHILDREN	   = 1,
      OPC_BROWSE_ISITEM	         = 2
   }



   /// <summary>
   /// OPC defined item quality values
   /// </summary>
   public enum OPCQuality : short 
   {
      BAD				= 0,
      UNCERTAIN		= 0x40,
      GOOD			   = 0xC0,
      CONFIG_ERROR	= 0x4,
      NOT_CONNECTED	= 0x8,
      DEVICE_FAILURE	= 0xC,
      SENSOR_FAILURE	= 0x10,
      LAST_KNOWN		= 0x14,
      COMM_FAILURE	= 0x18,
      OUT_OF_SERVICE	= 0x1C,
      LAST_USABLE		= 0x44,
      SENSOR_CAL		= 0x50,
      EGU_EXCEEDED	= 0x54,
      SUB_NORMAL		= 0x58,
      LOCAL_OVERRIDE	= 0xD8
   }

   /// <summary>
   /// HRESULTS enumerator. Defines the OPC error codes.
   /// </summary>
   public class HRESULTS
   {
      public static bool Failed( int hresultcode )
      {	return (hresultcode < 0);	}

      public static bool Succeeded( int hresultcode )
      {	return (hresultcode >= 0);	}

      public const int S_OK						= 0x00000000;
      public const int S_FALSE					= 0x00000001;

      public const int OPC_E_INVALIDHANDLE		= unchecked( (int)0xC0040001 );		// opcerror.h
      public const int OPC_E_BADTYPE				= unchecked( (int)0xC0040004 );
      public const int OPC_E_PUBLIC				   = unchecked( (int)0xC0040005 );
      public const int OPC_E_BADRIGHTS			   = unchecked( (int)0xC0040006 );
      public const int OPC_E_UNKNOWNITEMID		= unchecked( (int)0xC0040007 );
      public const int OPC_E_INVALIDITEMID		= unchecked( (int)0xC0040008 );
      public const int OPC_E_INVALIDFILTER		= unchecked( (int)0xC0040009 );
      public const int OPC_E_UNKNOWNPATH			= unchecked( (int)0xC004000A );
      public const int OPC_E_RANGE				   = unchecked( (int)0xC004000B );
      public const int OPC_E_DUPLICATENAME		= unchecked( (int)0xC004000C );
      public const int OPC_S_UNSUPPORTEDRATE		= unchecked( (int)0x0004000D );
      public const int OPC_S_CLAMP				   = unchecked( (int)0x0004000E );
      public const int OPC_S_INUSE				   = unchecked( (int)0x0004000F );
      public const int OPC_E_INVALIDCONFIGFILE	= unchecked( (int)0xC0040010 );
      public const int OPC_E_NOTFOUND				= unchecked( (int)0xC0040011 );
      public const int OPC_S_ALREADYACKED			= unchecked( (int)0x00040200 );
      public const int OPC_S_INVALIDBUFFERTIME	= unchecked( (int)0x00040201 );
      public const int OPC_S_INVALIDMAXSIZE		= unchecked( (int)0x00040202 );
      public const int OPC_E_INVALID_PID			= unchecked( (int)0xC0040203 );
      public const int OPC_E_INVALIDBRANCHNAME	= unchecked( (int)0xC0040203 );
      public const int OPC_E_INVALIDTIME			= unchecked( (int)0xC0040204 );
      public const int OPC_E_BUSY      			= unchecked( (int)0xC0040205 );
      public const int OPC_E_NOINFO    			= unchecked( (int)0xC0040206 );

      public const int E_NOTIMPL					   = unchecked( (int)0x80004001 );		// winerror.h
      public const int E_NOINTERFACE				= unchecked( (int)0x80004002 );
      public const int E_ABORT					   = unchecked( (int)0x80004004 );
      public const int E_FAIL						   = unchecked( (int)0x80004005 );
      public const int E_OUTOFMEMORY				= unchecked( (int)0x8007000E );
      public const int E_INVALIDARG				   = unchecked( (int)0x80070057 );
      public const int E_EXCEPTION				   = unchecked( (int)0x80010105 );

      public const int CONNECT_E_NOCONNECTION	= unchecked( (int)0x80040200 );		// olectl.h
      public const int CONNECT_E_ADVISELIMIT		= unchecked( (int)0x80040201 );

   }	// class 


   //======================================================= OPC AE Definitions
   /// <summary>
   /// AEConditionState objects are returned in the GetConditionState client call
   /// for each requested event attribute.
   /// </summary>
   public class AEConditionState
   {
      /// <summary>
      /// A bit mask of three bits specifying the new state of the condition
      /// as a combination of ConditionState enumerator values.
      /// </summary>
      public ConditionState   State;
      /// <summary>
      /// The name of the currently active sub-condition, for multi-state conditions which are active. 
      /// For a single-state condition, this contains the condition name.
      /// For inactive conditions, this value is null (Nothing).
      /// </summary>
      public string   ActiveSubCondition;
      /// <summary>
      /// An expression which defines the sub-state represented by the szActiveSubCondition, for multi-state conditions. 
      /// For a single state condition, the expression defines the state represented by the condition.
      /// For inactive conditions, this value is null (Nothing).
      /// </summary>
      public string   ASCDefinition;
      /// <summary>
      /// The severity of any event notification generated on behalf of the szActiveSubCondition (1..1000). 
      /// The enumerator OpcSeverityLevel defines the base values of the defined levels. 
      /// For inactive conditions, this value is 1.
      /// </summary>
      public int      ASCSeverity;
      /// <summary>
      /// The text string to be included in any event notification generated on behalf of the szActiveSubCondition.
      /// For inactive conditions, this value is null (Nothing).
      /// </summary>
      public string   ASCDescription;
      /// <summary>
      /// Quality associated with the condition state. 
      /// Values are as defined for the OPC Quality Flags in the OPC Data Access Server specification.
      /// </summary>
      public short    Quality;
      /// <summary>
      /// The time of the most recent acknowledgment of this condition (of any sub-condition).
      /// Contains DateTime.MinValue if the condition has never been acknowledged.
      /// </summary>
      public DateTime LastAckTime;
      /// <summary>
      /// Time of the most recent transition into ActiveSubCondition. 
      /// This is the time value which must be specified when acknowledging the condition.
      /// Contains DateTime.MinValue if the condition has never been active.
      /// </summary>
      public DateTime SubCondLastActive;
      /// <summary>
      /// Time of the most recent transition into the condition. 
      /// There may be transitions among the sub-conditions which are more recent.
      /// Contains DateTime.MinValue if the condition has never been active.
      /// </summary>
      public DateTime CondLastActive;
      /// <summary>
      /// Time of the most recent transition out of this condition.
      /// Contains DateTime.MinValue if the condition has never been active, 
      /// or if it is currently active for the first time and has never been exited.
      /// </summary>
      public DateTime CondLastInactive;
      /// <summary>
      /// This is the ID of the client who last acknowledged this condition.
      /// Contains null (Nothing) if the condition has never been acknowledged.
      /// </summary>
      public string   AcknowledgerID;
      /// <summary>
      /// The comment string passed in by the client who last acknowledged this condition.
      /// Contains null (Nothing) if the condition has never been acknowledged.
      /// </summary>
      public string   Comment;
      /// <summary>
      /// The number of sub-conditions defined for this condition. For multi-state conditions, 
      /// this value will be greater than one. For single-state conditions, this value will be 1.
      /// </summary>
      public int      NumSCs;
      /// <summary>
      /// Array of sub-condition names defined for this condition. For single-state conditions, 
      /// the array will contain one element, the value of which is the condition name.
      /// </summary>
      public string[] SCNames;
      /// <summary>
      /// Array of sub-condition definitions.
      /// </summary>
      public string[] SCDefinitions;
      /// <summary>
      /// Array of sub-condition severities.
      /// </summary>
      public int[]    SCSeverities;
      /// <summary>
      /// Array of sub-condition definitions.
      /// </summary>
      public string[] SCDescriptions;
      /// <summary>
      /// The length of the arrays EventAttributes and Errors. 
      /// Must be equal to NumEventAttrs passed into function GetConditionState().
      /// </summary>
      public int      NumEventAttrs;
      /// <summary>
      /// Array of vendor specific attributes associated with that latest event notification 
      /// for this condition. The order of the items returned matches the order that was specified 
      /// by AttributeIDs. 
      /// If a server cannot provide reasonable data for an attribute, the returned object should 
      /// be set to VT_EMPTY.
      /// </summary>
      public object[] EventAttributes;
      /// <summary>
      /// Array of HRESULT values for each requested attribute ID specified by AttributeIDs. 
      /// Servers should return S_OK if the Attribute ID is valid or E_FAIL if not.
      /// </summary>
      public int[]    Errors;


      //---------------------------------------------------
      /// <summary>
      /// Constructor, setting save default values.
      /// </summary>
      public AEConditionState()
      {
         LastAckTime = DateTime.MinValue ;
         SubCondLastActive = DateTime.MinValue ;
         CondLastActive = DateTime.MinValue ;
         CondLastInactive = DateTime.MinValue ;
         NumSCs = 1;
      }

   }


   /// <summary>
   ///  OPC recommended base values for OPC severity levels.
   /// </summary>
   public enum OpcSeverityLevel
   {
      Minimum = 1,
      Low = 1,
      MediumLow = 201,
      Medium = 401,
      MediumHigh = 601,
      High = 801,
      Maximum = 1000
   }
 
   /// <summary>
   /// Event Types
   /// </summary>
   [Flags]
   public enum EventType
   {
      /// <summary>
      /// Simple event.
      /// </summary>
      SimpleEvent = 1,
      /// <summary>
      /// Tracking event.
      /// </summary>
      TrackingEvent = 2,
      /// <summary>
      /// Condition-Related event.
      /// </summary>
      ConditionEvent = 4,
      /// <summary>
      /// All event types
      /// </summary>
      All = 7
   }


   /// <summary>
   /// The Change Mask defines flag bits.<br/>
   /// It is used in OnEvent callbacks to indicates to the client which properties of the 
   /// condition have changed, to have caused the server to send the event notification. 
   /// One or more bits may be set in a mask.
   /// </summary>
   [Flags]
   public enum EventChangeMask
   {
      /// <summary>
      /// The condition�s active state has changed.
      /// </summary>
      ActiveState = 1,
      /// <summary>
      /// The condition�s acknowledgment state has changed.
      /// </summary>
      AckState = 2,
      /// <summary>
      /// The condition�s enabled state has changed.
      /// </summary>
      EnableState = 4,
      /// <summary>
      /// The ConditionQuality has changed.
      /// </summary>
      Quality = 8,
      /// <summary>
      /// The severity level has changed.
      /// </summary>
      Severity = 16,
      /// <summary>
      /// The condition has transitioned into a new sub-condition.
      /// </summary>
      SubCondition = 32,
      /// <summary>
      /// The event message has changed (compared to prior event notifications related to this condition).
      /// </summary>
      Message = 64,
      /// <summary>
      /// One or more event attributes have changed (compared to prior event notifications related to this condition).
      /// </summary>
      Attribute = 128,
   }


   /// <summary>
   /// Event Server condition state.<br/>
   /// The ConditionState defines flag bits that are used to indicate the state of the conditon. 
   /// Multiple bits may be set in any combination.
   /// </summary>
   [Flags]
   public enum ConditionState
   {
      /// <summary>
      /// The condition has been enabled.
      /// </summary>
      Enabled = 1,
      /// <summary>
      /// The condition has become active.
      /// </summary>
      Active = 2,
      /// <summary>
      /// The condition has been acknowledged.
      /// </summary>
      Acked = 4,
   }


   /// <summary>
   /// Types of filtering an event server may support
   /// </summary>
   [Flags]
   public enum EventFilter
   {
      /// <summary>
      /// The server supports filtering by event type.
      /// </summary>
      ByEvent = 1,
      /// <summary>
      /// The server supports filtering by event categories.
      /// </summary>
      ByCategory = 2,
      /// <summary>
      /// The server supports filtering by severity levels.
      /// </summary>
      BySeverity = 4,
      /// <summary>
      /// The server supports filtering by process area.
      /// </summary>
      ByArea = 8,
      /// <summary>
      /// The server supports filtering by event sources.
      /// </summary>
      BySource = 16
   }
   

   /// <summary>
   /// Data passed to the client in an event callback
   /// </summary>
   public class EventData
   {
      /// <summary>
      /// Only for Condition events.
      /// Indicates to the client which properties of the condition have changed, to have caused the 
      /// server to send the event notification. It may have one or more of the EventChangeMask enumerator values.
      /// If the event notification is the result of a Refresh, these bits are to be ignored.
      /// For a 'new event', EventChangeMask.ActivState is the only bit which will always be set. 
      /// Other values are server specific. (A 'new event' is any event resulting from the related condition 
      /// leaving the Inactive and Acknowledged state.)
      /// </summary>
      public int      ChangeMask;
      /// <summary>
      /// Only for Condition events.
      /// A bit mask of ConditionState enumerator values specifying the new state of the condition.
      /// See OPC spec section 2.4.9 and Figure 2-2 for exactly which state transitions generate event notifications.
      /// </summary>
      public int      NewState;
      /// <summary>
      /// The source of event notification. This Source can be used in the TranslateToItemIDs method to determine any 
      /// related OPC Data Access itemIDs.
      /// </summary> 
      public string   Source;
      /// <summary>
      /// Time of the event occurrence - for conditions, time that the condition transitioned into the new state or sub-condition. 
      /// For example, if the event notification is for acknowledgment of a condition, this would be the time that the 
      /// condition became acknowledged.
      /// </summary>
      public DateTime Time;
      /// <summary>
      /// Event notification message describing the event.
      /// </summary>
      public string   Message;
      /// <summary>
      /// A value of the EventType enumerator for Simple, Condition-Related, or Tracking events, respectively.
      /// </summary>
      public int      EventType;
      /// <summary>
      /// Standard and Vendor-specific event category codes.
      /// </summary>
      public int      EventCategory;
      /// <summary>
      /// Event severity (1..1000).
      /// </summary>
      public int      Severity;
      /// <summary>
      /// Only for Condition events.
      /// The name of the condition related to this event notification.
      /// </summary>
      public string   ConditionName;
      /// <summary>
      /// Only for Condition events.
      /// The name of the current sub-condition, for multi-state conditions. 
      /// For a single-state condition, this contains the condition name.
      /// </summary>
      public string   SubconditionName;
      /// <summary>
      /// Only for Condition events.
      /// Quality associated with the condition state. 
      /// Values are as defined for the OPC Quality Flags in the OPC Data Access Server specification.
      /// </summary>
      public int      Quality;
      /// <summary>
      /// Only for Condition events.
      /// This flag indicates that the related condition requires acknowledgment of this event. 
      /// The determination of those events which require acknowledgment is server specific. 
      /// For example, transition into a LimitAlarm condition would likely require an acknowledgment, 
      /// while the event notification of the resulting acknowledgment would likely not require an acknowledgment.
      /// </summary>
      public bool     AckRequired;
      /// <summary>
      /// Time that the condition became active (for single-state conditions), or the time of the 
      /// transition into the current sub-condition (for multi-state conditions). 
      /// This time is used by the client when acknowledging the condition (see EventServer.AckCondition method).
      /// </summary>
      public DateTime ActiveTime;
      /// <summary>
      /// Only for Condition events.
      /// Server defined cookie associated with the event notification. 
      /// This value is used by the client when acknowledging the condition (see AckCondition method). 
      /// This value is opaque to the client.
      /// </summary>
      public int      Cookie;
      /// <summary>
      /// The length of the vendor specific event attribute array.
      /// </summary>
      public int      NumEventAttrs;
      /// <summary>
      /// Array of vendor specific event attributes returned for this event notification. 
      /// See the EventSubscription.SelectReturnedAttributes method.
      /// The order of the items returned matches the order that was specified by the select.
      /// </summary>
      public object[] EventAttributes;
      /// <summary>
      /// Only for Tracking events.
      /// For tracking events, this is the actor ID for the event notification.
      /// For condition-related events, this is the AcknowledgerID when OPC_CONDITION_ACKED is set in wNewState. 
      /// If the AcknowledgerID is a NULL string, the event was automatically acknowledged by the server.
      /// For other events, the value is null (Nothing).
      /// </summary>
      public string   ActorID;


      //------------------------------------------------------------
      /// <summary>
      /// Default constructor
      /// </summary>
      public EventData()
      {
         Time = DateTime.UtcNow ;
         ActiveTime = DateTime.MinValue ;
      }
   }

      
   //============================================================================================
   /// <summary>
   /// 	<para>This class defines the generic DA server interface. See chapter Server Structure
   ///     for an explanation of the server data and thread structure.</para>
   /// 	<para>The GenericServer class provides a set of generic DA server callback methods.
   ///     These methods can be used to read information from the generic server or change
   ///     data in the generic DA server.</para>
   /// 	<para>It also defines classes and enumerators used in the data exchange with the
   ///     generic server and contains a standard implementation of the methods called by the
   ///     generic DA server.</para>
   /// 	<para>The class AppPlugin inherits from this class and defines method overloads for
   ///     the methods that need to be implemented for a specific application.</para>
   /// </summary>
   public class GenericServer
   {


      //=======================================================================================
      // Gerneric server callback methods

      //-------------------------------------------------------------------------------------------
      /// <summary>
      /// 	<para>Generic server callback method.</para>
      /// 	<para>Add an item in the generic server cache.<br/>
      ///     This method is typically called during the execution of the CreateServerItems or
      ///     ValidateItems methods, but it can be called anytime an item needs to be added to the
      ///     generic server cache.</para>
      /// 	<para>Item that are added to the generic server cache can be accessed from the OPC
      ///     clients.</para>
      /// </summary>
      /// <returns>Returns S_OK if the item was successfully added to the cache.</returns>
      /// <param name="AppItemHandle">
      /// Handle defined int the adaptation assembly to reference the item.<br/>
      /// This handle is passed as an item identifier in calls between the generic server and the
      /// customization plugin. It should be chosen so that it allows fast access to the item
      /// data.
      /// </param>
      /// <param name="ItemId">
      /// Fully qualified item name. The OPC clients use this name to access the
      /// item.<br/>
      /// This is a fully qualified item identifier such as "device1.channel5.heater3". The
      /// generic server part builds an appropriate hierarchical address space.<br/>
      /// Note that the separator character ( . in the above sample identifier ) must match the
      /// separator character specified in the GetServerParameters method!
      /// </param>
      /// <param name="AccessRights">
      /// Access rights as defined in the OPC specification: OPC.READABLE , OPC.WRITEABLE ,
      /// OPC.READWRITEABLE
      /// </param>
      /// <param name="InitValue">
      /// Object with initial value and the item's canonical data type.<br/>
      /// A value must always be specified for the canonical data type to be defined.
      /// </param>
      /// <param name="quality">
      /// Initial quality of the item. This is a short value ( Int16) with the value of the
      /// OPCQuality enumerator.
      /// </param>
      /// <param name="timestamp">Initial timestamp</param>
      /// <param name="scanRate">
      /// Scan rate for this item in ms. A value of -1 indicates and undefined value.<br/>
      /// This argument is used only be the generic server Professional Edition to optimize the
      /// item refresh.
      /// </param>
      public static int AddItem( int AppItemHandle, string ItemId, int AccessRights, 
         object InitValue, short quality, DateTime timestamp, int scanRate )
      {
         return cbAddItem( AppItemHandle, ItemId, AccessRights, InitValue, quality, timestamp, scanRate );
      }



      //--------------------------------------------------------------------------------------------
      /// <summary>
      /// Generic server callback method. Supported only by the Professional Edition generic server V5.1 or newer.<br/>
      /// The specified item is deleted in the generic server cache. 
      /// Only items that are not currently referenced by any client (not added to a group by a client) can be deleted.
      /// </summary>
      /// <param name="appItemHandle">The handle that was specified when the item was added.</param>
      /// <returns>Success/failure code:<br/>
      /// S_OK (0x0) the item was deleted<br/>
      /// OPC_E_NOTFOUND (0xC0040011) an item with the specified handle couldn't be found<br/>
      /// OPC_E_BADRIGHTS (0xC0040006) the item is in use and could not be deleted</returns>
      public static int DeleteItem(int appItemHandle)
      {
         return cbDeleteItem( appItemHandle );
      }


      //--------------------------------------------------------------------------------------------
      /// <summary>
      /// 	<para>Generic server callback method.</para>
      /// 	<para>Write an item value into the cache.</para>
      /// </summary>
      /// <returns>Returns S_OK if the value was successfully written into the cache.</returns>
      /// <param name="AppItemHandle">Item handle as defined in the AddItem method call.</param>
      /// <param name="newValue">
      /// Object with new item value. The value must match the canonical data type of this
      /// item. The canonical date type is the type of the value in the cache and is defined in
      /// the AddItem method call.<br/>
      /// null can be passed to change only the quality and timestamp.
      /// </param>
      /// <param name="quality">
      /// New quality of the item value. This is a short value ( Int16) with a value of the
      /// OPCQuality enumerator.
      /// </param>
      /// <param name="timestamp">Timestamp of the new item value.</param>
      public static int SetItemValue( int AppItemHandle, object newValue, short quality, DateTime timestamp )
      {
         int rtc ;
         mtxSetVal.WaitOne();
         try
         {
            if( cbSetItemValue == null )
               return HRESULTS.E_NOINTERFACE ;
            rtc = cbSetItemValue( AppItemHandle, newValue, quality, timestamp );
         }
         catch
         {
            rtc = HRESULTS.E_EXCEPTION ;
         }
         mtxSetVal.ReleaseMutex();
         return rtc ;
      }

      //--------------------------------------------------------------------------------------------
      /// <summary>
      /// 	<para>Generic server callback method.</para>
      /// 	<para>Set the OPC server state value, that is returned in client GetStatus
      ///     calls.</para>
      /// </summary>
      /// <returns>Always S_OK</returns>
      /// <param name="newState">An OpcServerState enumerator value.</param>
      public static void SetServerState( OpcServerState newState )
      {
         cbSetServerState( (int)newState );
      }


      //----------------------------------------------------------------------
      /// <summary>
      /// 	<para>Generic server callback method.</para>
      /// 	<para>Get a list of items that currently need beeing refresh, from the generic
      ///     server.</para>
      /// 	<para>
      /// 		<para>A refresh is needed when:<br/>
      ///         - the timestamp is older then the maxAge argument<br/>
      ///         - Professional version only:<br/>
      ///         - the item is used by at least on client and the timestamp is older then the
      ///         item defined maxAge. The item maxAge is the client defined sampling rate, if
      ///         defined, otherwise the plug-in defined sampling rate is used. If this is
      ///         undefined also then the maxAge argument is used.</para>
      /// 	</para>
      /// </summary>
      /// <returns>Always returns S_OK</returns>
      /// <param name="mode">
      /// 	<para>Controls how the generic server Professional Edition determines if the item
      ///     needs refresh.<br/>
      ///     0: return all items with exeeded maxAge<br/>
      ///     1: return only items with activeCount&gt;0 and exceeded maxAge</para>
      /// </param>
      /// <param name="maxAge">MaxAge time in ms. </param>
      /// <param name="hnd">Array of handles of the items that need to be refreshed.</param>
      public static void GetRefreshNeed( int mode, int maxAge, out int[] hnd )
      {
         IntPtr items ;
         int numHnd;
         if( cbGetRefreshNeed == null )
         {
            hnd = new int[0];
            return ;
         }
         cbGetRefreshNeed( mode, maxAge, out numHnd, out items );
         hnd = new int[ numHnd ];
         for (int i = 0; i < numHnd; ++i)
         {
            hnd[i] = Marshal.ReadInt32(items, i * Marshal.SizeOf(typeof(int)) );
         }
         Marshal.FreeHGlobal( items );
      }

      //----------------------------------------------------------------------
      /// <summary>
      /// 	<para>Generic server callback method.</para>
      /// 	<para>Request the server to shutdown. Supported in the Professional version only.
      ///     The server application terminates without returning. The plugin needs to terminate
      ///     all threads and release all resources before this method is called. The generic
      ///     server notifies all clients and then terminates the application.</para>
      /// </summary>
      public static void ShutDownRequest( string reason )
      {
         cbShutDownRequest( reason );
      }



      //----------------------------------------------------------------------
      /// <summary>
      /// 	<para>Generic server callback method. Supported in the Professional version
      ///     only.</para>
      /// 	<para>Get information about the current server status.</para>
      /// </summary>
      /// <param name="state">Current server status</param>
      /// <param name="instanceCount">current number of server instances ( connected clients)</param>
      /// <param name="groupCount">Array with the current number of groups for each currently
      /// existing server instance (connected client).</param>
      public static void GetServerInfo( out OpcServerState state, out int instanceCount, out int[] groupCount )
      {
         int iState;
         IntPtr items ;
         if( cbGetServerInfo == null )
         {
            state = OpcServerState.OPC_STATUS_NOCONFIG ;
            instanceCount = 0;
            groupCount = null ;
            return ;
         }

         cbGetServerInfo( out iState, out instanceCount, out items );
         state = (OpcServerState) iState ;

         groupCount = new int[ instanceCount ];
         for( int i=0 ; i<instanceCount ; ++i )
         {
            groupCount[i] = Marshal.ReadInt32( items, i * Marshal.SizeOf(typeof(int)) );
         }
         Marshal.FreeHGlobal( items );
      }



      //----------------------------------------------------------------------
      /// <summary>
      /// Generic server callback method. Supported only by the Professional Edition generic server V5.1 or newer.<br/>
      /// Get information about all server instances or the requested server instance only. 
      /// This information may be used to identify the client application, especially when the 
      /// client application defines a name using the SetClientName OPC method.<br/>
      /// Be aware the the client name cannot yet be defined during the instance creation 
      /// (ServerInstancesChange call for creation). 
      /// The client application has to call SetClientName() to define the name.
      /// The client handle is passed as an argument to the plugin methods ValidateItems and
      /// WriteItems to enable the server to allow/deny the access based on the instance information.
      /// </summary>
      /// <param name="handle">Handle of the server instance or 0 for all instances</param>
      /// <param name="info">Current server instance data.
      /// The array lenght is 0 if the requested instance doesn't exist.</param>
      public static int GetServerInstanceInfo(int handle, out ServerInstanceInfo[] info )
      {
         info = null;
         if (cbGetServerInstanceInfo == null)   // not supported in generic server
            return HRESULTS.E_NOTIMPL;

         IntPtr iinf;
         int instanceCount;
         int rtc = cbGetServerInstanceInfo( handle, out instanceCount, out iinf);
         if (rtc != HRESULTS.S_OK)
            return rtc;

         info = new ServerInstanceInfo[instanceCount];
         long ph = (long)iinf;
         for (int i = 0; i < instanceCount; ++i)
         {
            info[i] = (ServerInstanceInfo)Marshal.PtrToStructure( (IntPtr)ph, typeof(ServerInstanceInfo) );
            ph += Marshal.SizeOf(typeof(ServerInstanceInfo));
         }
         Marshal.FreeHGlobal(iinf);
         return HRESULTS.S_OK;
      }


      //----------------------------------------------------------------------
      /// <summary>
      /// 	<para>Generic server callback method.</para>
      /// 	<para>Get information about the specified item from the generic server cache. 
      /// 	Can also be used to check if the item already exists in the generic server.</para>
      /// </summary>
      /// <returns>Always returns S_OK</returns>
      /// <param name="itemID">Fully qualifid name of the item to be queried.</param>
      /// <param name="itemInfo">Item definition and status information</param>
      public static int GetItemInfo( string itemID, out CacheItemInfo itemInfo )
      {
         itemInfo = new CacheItemInfo();
         int GroupLinkCounter; 
         int GroupActiveLinkCounter;
         int AppHandle;
         int ScanRate;
         int SamplingRate;
         Type DataType;
         int rtc = cbGetItemInfo( itemID, out GroupLinkCounter, out GroupActiveLinkCounter,
            out AppHandle, out ScanRate, out SamplingRate, out DataType );
         if( HRESULTS.Failed(rtc) )
            return rtc;

         itemInfo.AppHandle = AppHandle ;
         itemInfo.DataType  = DataType ;
         itemInfo.GroupActiveLinkCounter = GroupActiveLinkCounter;
         itemInfo.GroupLinkCounter = GroupLinkCounter;
         itemInfo.SamplingRate = SamplingRate ;
         itemInfo.ScanRate = ScanRate;
         return HRESULTS.S_OK ;
      }


      //-----------------------------------------------------  callback methods
      static private AddItem            cbAddItem ;
      static private DeleteItem         cbDeleteItem ;
      static private SetServerState     cbSetServerState ;
      static private ShutDownRequest    cbShutDownRequest ;
      static private SetItemValue       cbSetItemValue ;
      static private GetRefreshNeed     cbGetRefreshNeed ;
      static private GetServerInfo      cbGetServerInfo ;
      static private GetItemInfo        cbGetItemInfo;
      static private GetServerInstanceInfo cbGetServerInstanceInfo;

      //------------------------------------------------------- Mutex definitions
      static private Mutex              mtxSetVal = new Mutex(false);
      
      //------------------------------------------------------- DA Item configuration
      static internal SrvRegDef         DAServerDef ;
      static internal string            TraceLog = null;





      //==========================================================================================
      // Default implementations of methods called from the generic server


      //-----------------------------------------------------------------------------------------------
      /// <summary>
      /// 	<para>This method is called from the generic server at startup. It passes the
      ///     callback methods supported by the generic server. These callback methods can be
      ///     called anytime to exchange data with the generic server.</para>
      /// 	<para>The DefineCallbacks method must not be overloaded or changed. The default
      ///     implementation stores the delegates for later callbacks.</para>
      /// </summary>
      /// <param name="dlgAddItem">Add an item to the server's address space</param>
      /// <param name="dlgDeleteItem">Remove an item from the server's address space. NOT supported in this version.</param>
      /// <param name="dlgShutDownRequest">Initiates the server shutdown.</param>
      /// <param name="dlgSetItemValue">Writes a new item value into the server's cache</param>
      /// <param name="dlgGetRefreshNeed">Returns an array of handles of the items that need be refreshed.</param>
      /// <param name="dlgSetServerState">Returns information about the current server state.</param>
      /// <param name="dlgGetServerInfo">Returns information about the current server usage.</param>
      /// <param name="dlgGetItemInfo">Returns information about an item.</param>
      public void DefineCallbacks( AddItem dlgAddItem, DeleteItem dlgDeleteItem, ShutDownRequest dlgShutDownRequest,
         SetItemValue dlgSetItemValue, GetRefreshNeed dlgGetRefreshNeed, 
         SetServerState dlgSetServerState, GetServerInfo dlgGetServerInfo, GetItemInfo dlgGetItemInfo )
      {
         cbAddItem         = dlgAddItem;
         cbDeleteItem      = dlgDeleteItem;
         cbShutDownRequest = dlgShutDownRequest;
         cbSetItemValue    = dlgSetItemValue;
         cbGetRefreshNeed  = dlgGetRefreshNeed;
         cbSetServerState  = dlgSetServerState;
         cbGetServerInfo   = dlgGetServerInfo;
         cbGetItemInfo     = dlgGetItemInfo;
      }


      //-----------------------------------------------------------------------------------------------
      /// <summary>
      /// 	<para>This method is called from the professional edition generic server V5.1 an newer at startup. 
      /// It passes an additional callback methods supported by the professional edition generic server. 
      /// The callback method can be called anytime to exchange data with the generic server.</para>
      /// <para>The DefineCallbacks2 method must not be overloaded or changed. The default
      ///     implementation stores the delegates for later callbacks.</para>
      /// </summary>
      /// <param name="dlgGetServerInstanceInfo"></param>
      public void DefineCallbacks2( GetServerInstanceInfo dlgGetServerInstanceInfo)
      {
         cbGetServerInstanceInfo = dlgGetServerInstanceInfo;
      }


      //====================================================================
      /// <summary>
      /// 	<para>This method is called from the generic server at the startup, when the first
      /// client connects. All items supported by the server need to be defined by calling the
      /// AddItem callback method for each item.</para>
      /// 	<para>The Item IDs are fully qualified names ( e.g. Dev1.Chn5.Temp )<br/>
      /// The generic server part creates an approriate hierachical address space.
      /// The sample code defines the application item handle as the the buffer array index.
      /// This handle is passed in the calls from the generic server to identify the item. 
      /// It should allow quick access to the item definition / buffer. 
      /// The handle may be implemented differently depending on the application.</para>
      /// 	<para>The branch separator character used in the fully qualified item name must match
      /// the separator character defined in the GetServerParameters method.</para>
      /// </summary>
      /// <remarks>
      /// If the item definitions are read from an XML configuration file and this file also contains the 
      /// BranchSeperator character definition then the file must be loaded in
      /// the GetServerParameters method because this method is called first and needs the 
      /// BranchSeperator character definition.
      /// </remarks>
      /// <param name="cmdParams">
      /// String with the command line parameters as they were specified when the server
      /// was being registered. The XDARap XML DA server does not support this feature and calls
      /// always with an empty string.
      /// </param>
      public int CreateServerItems( string cmdParams ) 
      {
         // server has no items defined
         return HRESULTS.S_OK;
      }

      //-----------------------------------------------------------------------
      /// <summary>
      /// 	<para>This method is called from the generic server at startup for normal
      ///     operation or for registration. It provides server registry information for this
      ///     application required for DCOM registration. The DANSrv registers the OPC server 
      ///     accordingly. The XDARap XML DA server ignores most of the registration information.<br/>
      ///     This method does typically not have to be changed. The wizard generated the GUIDs for 
      ///     this server and inserts the server names as defined in the wizard dialog.</para>
      /// 	<para>The default implementation in <em>IGeneric.cs/vb</em> initializes default values
      ///     and tries to read the configuration definitions from the file
      ///     <em>DANSrv.exe.config</em> respectively <em>web.config</em> for XML DA servers. 
      ///     The method can be replaced by defining an overload in
      ///     <em>ServerAdapt.cs/vb</em>.<br/>
      ///     The definitions can be made in the code to prevent them from being changed
      ///     without a recompilation.</para>
      /// </summary>
      /// <returns>Definition structure</returns>
      /// <remarks>
      /// 	<para>The default implementation in IGeneric.cs/vb tries to read the definitions for the
      /// 	following configuration definitions from the <em>DANSrv.exe.config</em> respectively 
      /// 	<em>web.config</em> file. The following are sample definitions values.</para>
      /// 	<para>&lt;add key="ClsidServer" value =
      ///     <em>"{84FC5BB0-8384-4742-BAA2-953ABF85F623}"</em> /&gt;<br/>
      ///      &lt;add key="ClsidApp" value =
      ///     <em>"{84FC5BB0-8384-4742-BAA2-953ABF85F623}"</em> /&gt;<br/>
      ///      &lt;add key="ServerProgID" value = "<em>Advosol.DANSrv</em>" /&gt;<br/>
      ///      &lt;add key="CurrServerProgID" value = "<em>Advosol.DANSrv.1</em>" /&gt;<br/>
      ///      &lt;add key="ServerName" value = "<em>Advosol.DANSrv Sample</em>" /&gt;<br/>
      ///      &lt;add key="CurrServerName" value = "<em>Advosol.DANSrv Sample V1.0</em>" /&gt;  
      ///      The XDARap returns this definitionin the GetStatus client call.<br/>
      ///      &lt;add key="CompanyName" value = "<em>Advosol Inc."</em> /&gt;</para>
      /// 	<para>The CLSID definitions need to be unique and can be created with the Visual
      ///     Studio <em>Create GUID</em> tool.</para>
      /// 	<para>The DANSrv/XDARap project generation wizards create a file with unique CLSIDs and
      ///     the definitions from the wizard dialog. The default values in the code are the
      ///     same as in the config file.</para>
      /// </remarks>
      public SrvRegDef GetServerRegistryDef()
      {
         // each server needs unique CLSID ProgIG. 
         // This method must be overloaded or the config file have unique definitions.
         // Default settings:
         DAServerDef.ClsidServer      = "{84FC5BB0-8384-4742-BAA2-953ABF85F623}";   // CLSID of current Server
         DAServerDef.ClsidApp         = "{84FC5BB0-8384-4742-BAA2-953ABF85F623}";   // APPID of Application
         DAServerDef.PrgidServer      = "Advosol.DANSrv";                           // Version independent Prog.Id.
         DAServerDef.PrgidCurrServer  = "Advosol.DANSrv.1";                         // Prog Id. of current Server
         DAServerDef.NameServer       = "Advosol.DANSrv Sample";                    // Friendly name of server
         DAServerDef.NameCurrServer   = "Advosol.DANSrv Sample V1.0";              // Friendly name of Current server
         DAServerDef.CompanyName      = System.Diagnostics.FileVersionInfo.GetVersionInfo(
            System.Reflection.Assembly.GetExecutingAssembly().Location ).CompanyName;

         AppSettingsReader ar = new AppSettingsReader();
         object val= null;
         //------- Server CLSID
         try
         {
            val = null;
            val = ar.GetValue( "ClsidServer", typeof(string) );
         }
         catch
         {}
         if( val != null)	
            DAServerDef.ClsidServer = val.ToString();

         //------- Application CLSID
         try
         {
            val = null;
            val = ar.GetValue( "ClsidApp", typeof(string) );
         }
         catch
         {}
         if( val != null)	
            DAServerDef.ClsidApp = val.ToString();

         //------- Server ProgID
         try
         {
            val = null;
            val = ar.GetValue( "ServerProgID", typeof(string) );
         }
         catch
         {}
         if( val != null)	
            DAServerDef.PrgidServer = val.ToString();

         //------- Server ProgID current version
         try
         {
            val = null;
            val = ar.GetValue( "CurrServerProgID", typeof(string) );
         }
         catch
         {}
         if( val != null)	
            DAServerDef.PrgidCurrServer = val.ToString();

         //------- Server friendly name
         try
         {
            val = null;
            val = ar.GetValue( "ServerName", typeof(string) );
         }
         catch
         {}
         if( val != null)	
            DAServerDef.NameServer = val.ToString();

         //------- Server friendly name current version
         try
         {
            val = null;
            val = ar.GetValue( "CurrServerName", typeof(string) );
         }
         catch
         {}
         if( val != null)	
            DAServerDef.NameCurrServer = val.ToString();

         //------- Company name
         try
         {
            val = null;
            val = ar.GetValue( "CompanyName", typeof(string) );
         }
         catch
         {}
         if( val != null)	
            DAServerDef.CompanyName = val.ToString();

         return DAServerDef;
      }


      

      //-----------------------------------------------------------------------
      /// <summary>
      ///  Registration definitions for the Alarm/Event Server Option.<br/>
      /// 	<para>This method is called from the generic server at startup for normal
      ///     operation or for registration. It provides server registry information for this
      ///     application required for DCOM registration. The DANSrv registers the OPC server 
      ///     accordingly. The XDARap XML DA server ignores most of the registration information.<br/>
      ///     This method does typically not have to be changed. The wizard generated the GUIDs for 
      ///     this server and inserts the server names as defined in the wizard dialog.</para>
      /// 	<para>The default implementation in <em>IGeneric.cs/vb</em> initializes default values
      ///     and tries to read the configuration definitions from the file
      ///     <em>DANSrv.exe.config</em> respectively <em>web.config</em> for XML DA servers. 
      ///     The method can be replaced by defining an overload in
      ///     <em>ServerAdapt.cs/vb</em>.<br/>
      ///     The definitions can be made in the code to prevent them from being changed
      ///     without a recompilation.</para>
      /// </summary>
      /// <returns>Definition structure</returns>
      /// <remarks>
      /// 	<para>The default implementation in IGeneric.cs/vb tries to read the definitions for the
      /// 	following configuration definitions from the <em>DANSrv.exe.config</em> respectively 
      /// 	<em>web.config</em> file. The following are sample definitions values.</para>
      /// 	<para>&lt;add key="ClsidServer" value =
      ///     <em>"{604C23CE-816F-46ed-BCF9-0473A0A3617F}"</em> /&gt;<br/>
      ///      &lt;add key="ServerProgID" value = "<em>Advosol.DANSrv.AE</em>" /&gt;<br/>
      ///      &lt;add key="CurrServerProgID" value = "<em>Advosol.DANSrv.AE.1</em>" /&gt;<br/>
      ///      &lt;add key="ServerName" value = "<em>Advosol DANSrv AE Option</em>" /&gt;<br/>
      ///      &lt;add key="CurrServerName" value = "<em>Advosol DANSrv AE Option V1.0</em>" /&gt;  
      ///      The XDARap returns this definitionin the GetStatus client call.<br/>
      ///      &lt;add key="CompanyName" value = "<em>Advosol Inc."</em> /&gt;</para>
      /// 	<para>The CLSID definitions need to be unique and can be created with the Visual
      ///     Studio <em>Create GUID</em> tool.</para>
      /// 	<para>The DANSrv/XDARap project generation wizards create a file with unique CLSIDs and
      ///     the definitions from the wizard dialog. The default values in the code are the
      ///     same as in the config file.</para>
      /// </remarks>
      public SrvRegDef GetAEServerRegistryDef()
      {
         SrvRegDef Def = new SrvRegDef();
         // each server needs unique CLSID ProgIG. 
         // This method must be overloaded or the config file have unique definitions.
         // Default settings:
         Def.ClsidServer      = "{604C23CE-816F-46ed-BCF9-0473A0A3617F}";   // CLSID of current Server AppId
         Def.PrgidServer      = "Advosol.DANSrv.AE";        // Version independent Prog. ID
         Def.PrgidCurrServer  = "Advosol.DANSrv.AE.1";      // Prog. ID of current server
         Def.NameServer       = "Advosol DANSrv AE Option";         // Friendly name of server
         Def.NameCurrServer   = "Advosol DANSrv AE Option V1.0";       // Friendly name of current server

         AppSettingsReader ar = new AppSettingsReader();
         object val= null;
         //------- AE Server CLSID
         try
         {
            val = null;
            val = ar.GetValue( "ClsidAEServer", typeof(string) );
         }
         catch
         {}
         if( val != null)	
            Def.ClsidServer = val.ToString() ;

         //------- AE Server ProgID
         try
         {
            val = null;
            val = ar.GetValue( "AEServerProgID", typeof(string) );
         }
         catch
         {}
         if( val != null)	
            Def.PrgidServer = val.ToString() ;

         //------- AE Server ProgID current version
         try
         {
            val = null;
            val = ar.GetValue( "CurrAEServerProgID", typeof(string) );
         }
         catch
         {}
         if( val != null)	
            Def.PrgidCurrServer = val.ToString() ;

         //------- AE Server friendly name
         try
         {
            val = null;
            val = ar.GetValue( "AEServerName", typeof(string) );
         }
         catch
         {}
         if( val != null)	
            Def.NameServer = val.ToString() ;

         //------- AE Server friendly name current version
         try
         {
            val = null;
            val = ar.GetValue( "CurrAEServerName", typeof(string) );
         }
         catch
         {}
         if( val != null)	
            Def.NameCurrServer = val.ToString() ;

         return Def ;
      }

      //-----------------------------------------------------------------
      /// <summary>
      /// 	<para>This method is called from the generic server at startup, when the first
      ///     client connects.<br/>
      ///     It defines the application specific server parameters and operating modes.<br/>
      ///     The default implementation in <em>IGeneric.cs</em> initializes default values and
      ///     tries to read the configuration definitions from the file
      ///     <em>DANSrv.exe.config</em> respectively <em>web.config</em> for XML DA servers.</para>
      /// 	<para>The default method can be replaced by defining a method overload in
      ///     <em>ServerAdapt.cs</em>. The definitions can be made in the code to protect 
      ///     them from being changed without a recompilation.</para>
      /// </summary>
      /// <returns>Always returns S_OK</returns>
      /// <remarks>
      /// 	<para>The DANSrv/XDARap project generation wizards create a configuration file with the
      ///     definitions as specified in the wizard dialog. The default values in the code
      ///     are the same.</para>
      /// </remarks>
      /// <param name="UpdatePeriod">
      /// This interval in ms is used by the generic server as the fastest possible client update rate  
      /// and also uses this definition when determining the refresh need 
      /// if no client defined a sampling rate for the item.<br/>
      /// The default value is as defined in the wizard dialog. In the DANSrv.exe.config the
      /// value is defined as:<br/>
      /// &lt;add key="UpdatePeriod" value = <em>"100"</em> /&gt;
      /// </param>
      /// <param name="BrowseMode">
      /// 	<para>Defines how client browse calls are handled.<br/>
      ///  0 (real mode) : all browse calls are handled in the generic server according the items defined in the server cache.<br/>
      ///  2 (virtual mode) : all client browse calls are handled in this plug-in and
      ///     typically return the items that are or could be dynamically added to the server
      ///     cache.<br/>
      ///     Only the DANSrv/XDARap Professional Edition supports virtual mode browsing. The 
      ///     Standard Edition ignores this parameter.</para>
      /// 	<para>The default value is as defined in the wizard dialog. In the file 
      ///     <em>DANSrv.exe.config</em> respectively <em>web.config</em> the value is defined as:<br/>
      ///     &lt;add key="BrowseMode" value = <em>"REAL"</em> /&gt; &lt;!-- REAL or VIRTUAL
      ///     --&gt;</para>
      /// </param>
      /// <param name="validateMode">
      /// 	<para>This parameter controls how the generic server handles the ValidateItems
      ///     client calls.<br/>
      ///     0 : The plug-in ValidateItems method is called for items that are not found in the
      ///     generic server cache.<br/>
      ///     1 : The plug-in ValidateItems method is NEVER called. Requested items that are not
      ///     defined in the generic server cache are treated as unknown items.<br/>
      ///     2 : The plug-in ValidateItems method is ALWAYS called. This allows the plug-in to
      ///     determine the items access rights based on the client's credentials and return
      ///     OPC_E_UNKNOWNITEMID for clients without the required privileges.</para>
      /// 	<para>Only the DANSrv Professional Edition supports item validation and a dynamic
      ///     address space. The DANSrv Standard Edition ignores this parameter.</para>
      /// 	<para>The default value is as defined in the wizard dialog. In the file
      ///     <em>DANSrv.exe.config</em> respectively <em>web.config</em> the value is defined as:<br/>
      ///     &lt;add key="ValidateMode" value= <em>"NEVER"</em> /&gt; &lt;!-- NEVER,
      ///     UNKNOWNITEMS or ALWAYS --&gt;</para>
      /// </param>
      /// <param name="BranchDelemitter">
      /// 	<para>Character is used as the branch/item separator character in fully qualified
      ///     item names. It is typically '.' or '/'.<br/>
      ///     This character must match the character used in the fully qualified item IDs
      ///     specified in the AddItems method call.</para>
      /// 	<para>The default value is as defined in the wizard dialog. In the file
      ///     <em>DANSrv.exe.config</em> respectively <em>web.config</em> the value is defined as:<br/>
      ///     &lt;add key="BranchDelemitter" value= <em>"."</em> /&gt;</para>
      /// </param>
      public int GetServerParameters( out int UpdatePeriod, out int BrowseMode, out int validateMode, out char BranchDelemitter  ) 
      {
         // Default Values
         UpdatePeriod = 100 ;		            // ms
         BrowseMode = (int)BROWSEMODE.REAL ;        // browse the real address space (generic part internally)
         validateMode = (int)ValidateMode.Never ;   // never call Plugin.ValidateItems 
         BranchDelemitter = '.' ;

         AppSettingsReader ar = new AppSettingsReader();
         object val= null;
         //------- UpdatePeriod Definition
         try
         {
            val = null;
            val = ar.GetValue( "UpdatePeriod", typeof(int) );
            if( val != null)	
               UpdatePeriod = Convert.ToInt32(val) ;
         }
         catch
         {}

         //------ BowseMode definition
         try
         {
            val = null;
            val = ar.GetValue( "BrowseMode", typeof(string) );
            if( val != null)	
            {
               if( val.ToString().ToUpper() == "VIRTUAL" )
                  BrowseMode = (int)BROWSEMODE.VIRTUAL ;
            }
         }
         catch
         {}

         //------ ValidateMode definition
         try
         {
            val = null;
            val = ar.GetValue( "ValidateMode", typeof(string) );
            if( val != null)	
            {
               if( val.ToString().ToUpper() == "UNKNOWNITEMS" )
                  validateMode = (int)ValidateMode.UnknownItems ;
               else if( val.ToString().ToUpper() == "ALWAYS" )
                  validateMode = (int)ValidateMode.Always ;
            }
         }
         catch
         {}

         //------ BranchDelemitter definition
         try
         {
            val = null;
            val = ar.GetValue( "BranchDelemitter", typeof(char) );
            if( val != null)	
               BranchDelemitter = Convert.ToChar(val) ;
         }
         catch
         {}

         return HRESULTS.S_OK;
      }


      //-------------------------------------------------------------------
      /// <summary>
      /// 	<para>Optional method. Called from DANSrv/XDARap Professional Edition generic server only.<br/>
      ///     Get working mode definitions for the generic server.<br/>
      ///     The default implementation in <em>IGeneric.cs/vb</em> initializes default values and
      ///     tries to read the configuration definitions from the file
      ///     <em>DANSrv.exe.config</em> respectively <em>web.config</em> in XML DA servers.</para>
      /// 	<para>The default method can be replaced by defining an overload in
      ///     <em>ServerAdapt.cs/vb</em>. The definitions can be made in the code to prevent
      ///     them from being changed without a recompilation.</para>
      /// </summary>
      /// <returns>Always returns S_OK</returns>
      /// <param name="clientUpdateMode">
      /// 	<para>Client update mode handling:<br/>
      ///     0 (CachePoll): The client update thread periodically checks all items in active
      ///     groups for changes<br/>
      ///     1 (Queue): The cache write method adds changed items to the OPC group assigned
      ///     queue for each group having the item.</para>
      /// 	<para>The default value is 0 (CachePoll). In the DANSrv.exe.config the value can be
      ///     defined as:<br/>
      /// 		<em>&lt;add key="ClientUpdateMode" value="CachePoll"/&gt; &lt;!-- CachePoll (0) or
      ///     Queue (1) --&gt;<br/></em></para>
      /// </param>
      /// <param name="writeCacheUpdateMode">
      /// 	<para>Controls the cache update in the handling of client write calls:<br/>
      ///     0 (Generic): The cache is updated in the generic server after returning from the
      ///     customization WiteItems method. Items with write error are not updated in the
      ///     cache.<br/>
      ///     1 (Custom): The generic server does NOT update the cache. The customization module
      ///     has to update the cache by executing the SetItemValue callback method for each
      ///     written item.</para>
      /// 	<para>The default value is 0 (Generic). In the DANSrv.exe.config the value can be
      ///     defined as:<br/>
      /// 		<em>&lt;add key="WriteCacheUpdateMode" value="Generic"/&gt; &lt;!-- Generic (0) or
      ///     Custom (1) --&gt;</em></para>
      /// </param>
      /// <param name="exeStartupMode">Determines how the server starts when the EXE file is executed. 
      /// This definitin has no effect when DCOM starts the server due to a client connect.
      /// The possible values are defined in the enumerator ExeStartMode.</param>
      /// <param name="notYetUsed2">Can be used later without having to change the DLL interface</param>
      /// <param name="notYetUsed3">Can be used later without having to change the DLL interface</param>
      /// <param name="notYetUsed4">Can be used later without having to change the DLL interface</param>
      /// <param name="notYetUsed5">Can be used later without having to change the DLL interface</param>
      /// <param name="notYetUsed6">Can be used later without having to change the DLL interface</param>
      /// <returns>Always returns S_OK</returns>
      public int GetModeDefinitions( out int clientUpdateMode, out int writeCacheUpdateMode, 
         out int exeStartupMode, out int lookupMode,
         out int notYetUsed3, out int notYetUsed4, 
         out int notYetUsed5, out int notYetUsed6 ) 
      {
         // Default values:
         clientUpdateMode = (int)ClientUpdateHandlingMode.ManyChanges ; 
         writeCacheUpdateMode = (int)WriteCacheUpdateHandlingMode.GenericServer ;
         exeStartupMode = (int)ExeStartMode.Partial ;
         lookupMode = (int)LookupMode.Generic;
         notYetUsed3 = 0 ;
         notYetUsed4 = 0 ;
         notYetUsed5 = 0 ;
         notYetUsed6 = 0 ;

         AppSettingsReader ar = new AppSettingsReader();
         object val= null;
         //------ ClientUpdateMode definition
         try
         {
            val = null;
            val = ar.GetValue( "ClientUpdateMode", typeof(string) );
            if( val != null)	
            {
            if( val.ToString().ToUpper() == "MANYITEMS" )
                  clientUpdateMode = (int)ClientUpdateHandlingMode.ManyItems ;
            }
         }
         catch
         {}

         //------ WriteCacheUpdateMode definition
         try
         {
            val = null;
            val = ar.GetValue( "WriteCacheUpdateMode", typeof(string) );
            if( val != null)	
            {
               if( val.ToString().ToUpper() == "CUSTOM" )
                  writeCacheUpdateMode = (int)WriteCacheUpdateHandlingMode.Custom; 
            }
         }
         catch
         {}

         //------ ExeStartupMode definition
         try
         {
            val = null;
            val = ar.GetValue("ExeStartMode", typeof(string));
            if (val != null)
            {
               if (val.ToString().ToUpper() == "FULL")
                  exeStartupMode = (int)ExeStartMode.Full;
            }
         }
         catch
         { }

         return HRESULTS.S_OK;
      }


	   //----------------------------------------------------------------
	   /// <summary>
	   /// This method is called when:<br/>
	   /// a) A client connects and therefore a new server instance has to be created. 
	   /// If the method returns an error code then no instance is created and the
	   /// client connect is failed.
	   /// b) A server instance is terminating  (a client disconnects). This method cannot
	   /// prevent this. The call is to inform the plugin of the status change.
	   /// </summary>
	   /// <param name="action">1=new instance is requested, 2=an instance terminates</param>
      /// <param name="instanceHandle">indicates the server instance for a particular client.</param>
      /// <returns>S_OK to allow the creation of a new server instance, 
	   /// an error code to prevent it.</returns>
      public int ServerInstancesChange(int action, int instanceHandle)
	   {
		   return HRESULTS.S_OK ;     // allow change
	   }


      //----------------------------------------------------------------
      /// <summary>
      /// This method is called from the generic server when a Shutdown is executed.<br/>
      /// To ensure proper process shutdown, any communication channels should 
      /// be closed and all threads terminated before this method returns.
      /// </summary>
      /// <returns>Always returns S_OK</returns>
      public void  ShutdownSignal()
      {
         // no action required in the default implementation
      }
 

      //-----------------------------------------------------------------
      /// <summary>
      /// Query the properties defined for the specified item
      /// </summary>
      /// <param name="ItemHandle">Application item handle</param>
      /// <param name="numProp">Number of properties retruned</param>
      /// <param name="IDs">Array with the the property ID number</param>
      /// <param name="Names">Array with the property names. This symbolic name is used only in the 
      /// XML-DA server. It is ignored by the DANSrv OPC DA server.</param>
      /// <param name="Descriptions">Array with the property description</param>
      /// <param name="Values">Array with the current property values</param>
      /// <returns>HRESULT success/error code. S_FALSE if the item has no custom properties.</returns>
      public int QueryProperties( int ItemHandle, 
         out int numProp, out int[] IDs, out string[] Names,
         out string[] Descriptions, out object[] Values ) 
      {
         // item has no custom properties
         numProp = 0 ;
         IDs = null;
         Names = null;
         Descriptions = null;
         Values = null ;
         return HRESULTS.S_FALSE ;
      }


      //-----------------------------------------------------------------
      /// <summary>
      /// Returns the values of the requested custom properties of the requested item.
      /// This method is not called for the OPC standard properties 1..6. These are handled
      /// in the generic server.
      /// </summary>
      /// <param name="ItemHandle">Item application handle</param>
      /// <param name="pID">ID of the property</param>
      /// <param name="Value">Property value</param>
      /// <returns>HRESULT success/error code. S_FALSE if the item has no custom properties.</returns>
      public int GetPropertyValue( int ItemHandle, int pID, out object Value ) 
      {
         // Item property is not available
         Value = null;
         return HRESULTS.OPC_E_INVALID_PID ;
      }

 
      //-----------------------------------------------------------------------------------
      /// <summary>
      /// Added in DANSrv V6.0<br/>
      /// This method is called by the generic server only when LookupMode is defined as 'PluginHandled'.
      /// If the request is invalid the method returns null.
      /// Otherwise, it needs to generate a unique name for the property item and create the property item by calling the generic server method AddItem().
      /// The plug-in must update the cache value of this item by calling the SetItemValue() method whenever the property value changes.
      /// </summary>
      /// <param name="instanceHandle">Handle the identifies the calling client application. 
      /// The method GetServerInstancesInfo can be used to get name information for this handle.</param>
      /// <param name="itemName">Fully qualified item ID.</param>
      /// <param name="propertyID">Identification number of the property to be assigned an item.</param>
      /// <returns>Fully qualified item ID of the created property item. NULL if the request is denied or failed.</returns>
      public string LookupItemID(int instanceHandle, string itemName, int propertyID)
      {
         return null;    // invalid request
      }



      //-------------------------------------------------------------------------------------
      /// <summary>
      /// Standard Edition Write handler.
      /// In the Professional Edition this overload is is only called if the method with the client handle argument is not defined.
      /// This method is called when a client executes a 'write' server call. 
      /// The items specified in the appHandles array need to be written to the device.<br/>
      /// The cache update handling depends on the configuration parameter writeCacheUpdateMode
      ///  For 0 (Generic): The cache is updated in the generic server after returning from the
      ///     customization WiteItems method. Items with write error are not updated in the
      ///     cache.<br/>
      ///  For 1 (Custom): The customization module needs to update the cache by executing the SetItemValue callback method for each
      ///     written item. The generic server does not update the cache.
      /// </summary>
      /// <param name="values">object with handle, value, quality, timestamp</param>
      /// <param name="errors">array with HRESULT success/error codes on return.</param>
      /// <returns>HRESULT success/error code</returns>
      public int WriteItems(DeviceItemValue[] values, out int[] errors)
      {
         // no item could be written
         errors = new int[values.Length];           // result array
         for (int i = 0; i < values.Length; ++i)       // init to S_OK
            errors[i] = HRESULTS.OPC_E_INVALIDHANDLE;
         return HRESULTS.S_FALSE;
      }

      //-------------------------------------------------------------------------------------
      /// <summary>
      /// Called from the DANSrv PROFESSIONAL EDITION ONLY!
      /// This method is called when a client executes a 'write' server call. 
      /// The items specified in the appHandles array need to be written to the device.<br/>
      /// The cache update handling depends on the configuration parameter writeCacheUpdateMode
      ///  For 0 (Generic): The cache is updated in the generic server after returning from the
      ///     customization WiteItems method. Items with write error are not updated in the
      ///     cache.<br/>
      ///  For 1 (Custom): The customization module needs to update the cache by executing the SetItemValue callback method for each
      ///     written item. The generic server does not update the cache.
      /// </summary>
      /// <param name="instanceHandle">Handle the identifies the calling client application. 
      /// The method GetServerInstancesInfo can be used to get name information for this handle.</param>
      /// <param name="values">object with handle, value, quality, timestamp</param>
      /// <param name="errors">array with HRESULT success/error codes on return.</param>
      /// <returns>HRESULT success/error code</returns>
      public int WriteItems(int instanceHandle, DeviceItemValue[] values, out int[] errors)
      {
         // no item could be written
         errors = new int[values.Length];           // result array
         for (int i = 0; i < values.Length; ++i)       // init to S_OK
            errors[i] = HRESULTS.OPC_E_INVALIDHANDLE;
         return HRESULTS.S_FALSE;
      }



      //-------------------------------------------------------------------------------------
      /// <summary>
      /// 	<para>Refresh the items listed in the appHandles array in the cache.</para>
      /// 	<para>This method is called when a client executes a read from device or a 
      /// 	read with maxAge argument<br/>
      /// 	For OPC DA V2 read from device calls the method is called with all client requested 
      /// 	items. In OPCDA V3 / XMLDA read calls the method is called only with the handles of 
      /// 	the items that are currently in the cache older than maxAge.</para>
      /// </summary>
      /// <param name="appHandles">Array with the application handle of the items that need to be refreshed.</param>
      /// <returns>Always returns S_OK</returns>
      public int RefreshItems( int[] appHandles )
      {
         // no items handled in this default implementation
         return HRESULTS.S_OK;
      }


      //-------------------------------------------------------------------------------
      // Dynamic address space handling                              ( Professional version only. )
      //-------------------------------------------------------------------------------
      /// <summary>
      /// Only the Professional Edition generic server calls this method.<br/>
      /// DEPRECIATED!  This overload is is only called if the overload with the instanceHandle argument is not defined. 
      /// In new applications implement the method overload with the instanceHandle parameter.<br/>
      /// This method is called when the client accesses items that do not yet exist in the server's cache.
      /// OPC DA V2 clients typically first call AddItems() or ValidateItems(). OPC DA V3 client may access
      /// items directly using the ItemIO read/write functions. In XML DA the calls directly specify the 
      /// item ID.<br/>
      /// If ValidateMode 'ALWAYS' is configured then the method is called for each client call, even 
      /// if the item exists in the server cache. The access can be denied for a specific client by 
      /// returning an error code in err[] for such items. 
      /// This module can:
      /// - add the item to the servers real address space and return success.
      ///   For each item to be added the callback method 'AddItem' has to be called.
      /// - return error for all or some items
      /// </summary>
      /// <param name="FullItemId">[in] string array with the names of the items to be validated</param>
      /// <param name="reason">ValidateReason enumerator indicating why the method is called. 
      /// See ValidateReason enumerator for valid values.</param>
      /// <param name="err">HRESULTS array with success/error code for each requested item</param>
      /// <returns>the number of successfully added items and/or accessible items.</returns>
      public int ValidateItems( string[] FullItemId, int reason, out int[] err )
      {
         // no valid items in this default implementation
         err = new int[ FullItemId.Length ];
         return 0 ;
      }

      //-------------------------------------------------------------------------------
      // Dynamic address space handling                              ( Professional version only. )
      //-------------------------------------------------------------------------------
      /// <summary>
      /// Only the Professional Edition generic server V5.1 or newer calls this method.<br/>
      /// This method is called when the client accesses items that do not yet exist in the server's cache.
      /// OPC DA V2 clients typically first call AddItems() or ValidateItems(). OPC DA V3 client may access
      /// items directly using the ItemIO read/write functions. In XML DA the calls directly specify the 
      /// item ID.<br/>
      /// If ValidateMode 'ALWAYS' is configured then the method is called for each client call, even 
      /// if the item exists in the server cache. The access can be denied for a specific client by 
      /// returning an error code in err[] for such items. 
      /// This module can:
      /// - add the item to the servers real address space and return success.
      ///   For each item to be added the callback method 'AddItem' has to be called.
      /// - return error for all or some items
      /// </summary>
      /// <param name="instanceHandle">Handle the identifies the calling client application. 
      /// The method GetServerInstancesInfo can be used to get name information for this handle.</param>
      /// <param name="FullItemId">[in] string array with the names of the items to be validated</param>
      /// <param name="reason">ValidateReason enumerator indicating why the method is called</param>
      /// <param name="err">HRESULTS array with success/error code for each requested item</param>
      /// <returns>the number of successfully added items and/or accessible items.</returns>
      public int ValidateItems(int instanceHandle, string[] FullItemId, int reason, out int[] err)
      {
         // no valid items in this default implementation
         err = new int[FullItemId.Length];
         return 0;
      }



      //===============================================================================
      // Browse the virtual server address space.                   ( Professional version only. )
      // All items that can be dynamically added to the server are returned.
      // This methods are only called when the BrowseModeVirtual is defined.
      //===============================================================================
  

      //------------------------------------------------------------------------
      /// <summary>
      /// 	<para>Virtual mode browse handling. </para>
      /// 	<para>Called only from the Professional edition generic server when BrowseMode VIRTUAL 
      /// 	is configured.</para>
      /// 	<para>Change the current browse branch to the specified branch in virtual address space.<br/>
      /// 	This method has to be implemented accordint the OPC DA V2 specification.</para>
      /// </summary>
      /// <returns>HRESULT error code</returns>
      /// <param name="dwBrowseDirection">browse to, up or down</param>
      /// <param name="Position">new absolute or relative branch</param>
      /// <param name="currBranch">current branch for the calling client</param>
      public int BrowseChangePosition( int dwBrowseDirection, string Position, ref string currBranch )
      {
         // not supported in this default implementation
         return HRESULTS.E_INVALIDARG ;
      }



      //-------------------------------------------------------------------------
      /// <summary>
      /// <para>Virtual mode browse handling.</para>
      /// <para>Called only from the Professional edition generic server when 
      /// BrowseMode VIRTUAL is configured.</para>
      /// <para>This method browses the items in the current branch of the virtual address space. 
      /// It has to be implemented according the OPC DA V2 specification.</para>
      /// </summary>
      /// <returns>
      /// 	<para>HRESULT error/success code</para>
      /// 	<para>
      /// 		<list type="table">
      /// 			<item>
      /// 				<term>
      /// 					<para><font size="1">E_FAIL<br/>
      ///                      E_OUTOFMEMORY<br/>
      ///                      E_INVALIDARG<br/>
      ///                      OPC_E_INVALIDFILTER<br/>
      ///                      S_OK<br/>
      ///                      S_FALSE</font></para>
      /// 				</term>
      /// 				<description><font size="1">The operation failed.<br/>
      ///                  Not enough memory<br/>
      ///                  An argument to the function was invalid.<br/>
      ///                  The filter string was not valid<br/>
      ///                  The operation succeeded.<br/>
      ///                  No items meet the filter criteria.<br/></font></description>
      /// 			</item>
      /// 		</list>
      /// 	</para>
      /// </returns>
      /// <param name="BrowseType">
      /// branch/leaf filter
      /// <a href="NSPlugin~NSPlugin.OPCBROWSETYPE.html">OPCBROWSETYPE</a><br/>
      /// OPC_BRANCH - returns only items that have children<br/>
      /// OPC_LEAF - returns only items that don't have children<br/>
      /// OPC_FLAT - returns everything at and below this level including all children of
      /// children - basically 'pretends' that the address space in actually FLAT<br/>
      /// This parameter is ignored for FLAT address space.
      /// </param>
      /// <param name="FilterCriteria">name pattern match expression</param>
      /// <param name="DataTypeFilter">
      /// Filter the returned list based in the available datatypes (those that would
      /// succeed if passed to AddItem). System.Void indicates no filtering.
      /// </param>
      /// <param name="AccessRightsFilter">
      /// Filter based on the AccessRights bit mask
      /// <a href="NSPlugin~NSPlugin.OPCACCESSRIGHTS.HTM">OPCAccess</a><br/>
      /// (OPC_READABLE or OPC_WRITEABLE). The bits passed in the bitmask are 'ANDed' with the
      /// bits that would be returned for this Item by AddItem, ValidateItem or
      /// EnumOPCItemAttributes. If the result is non-zero then the item is returned. A 0 value
      /// in the bitmask indicates that the AccessRights bits should be ignored during the
      /// filtering process.
      /// </param>
      /// <param name="currBranch">current branch for the calling client</param>
      /// <param name="NrItems">OUT: number of items returned</param>
      /// <param name="ItemIDs">OUT: Items meeting the browse criteria.</param>
      /// <returns>HRESULT success/error code</returns>
      public int BrowseItemIDs( int BrowseType, string FilterCriteria, System.Type DataTypeFilter,
         int AccessRightsFilter, string currBranch, out int NrItems, out string[] ItemIDs )
      {
         // not supported in this default implementation
         ItemIDs = null ;
         NrItems = 0 ;
         return HRESULTS.E_INVALIDARG ;
      }

      //----------------------------------------------------------------------------
      /// <summary>
      /// <para>Virtual mode browse handling.</para>
      /// <para>Called only from the Professional edition generic server when 
      /// BrowseMode VIRTUAL is configured.</para>
      /// <para>This method returns the fully qualified name of the specified item in the 
      /// current branch in the virtual address space. This name is used to add the item to 
      /// the real address space. 
      /// This method has to be implemented according the OPC DA V2 specification.</para>
      /// </summary>
      /// <param name="ItemName">Name of the item</param>
      /// <param name="currBranch">Fully qualified name of the current branch</param>
      /// <param name="FullItemID">OUT: Fully qualified name if the item. This name is used to access 
      /// the item or add it to a group.</param>
      /// <returns>HRESULT error/success code</returns>
      public int BrowseGetFullItemID( string ItemName, string currBranch, out string FullItemID )
      {
         // not supported in this default implementation
         FullItemID = null;
         return HRESULTS.E_INVALIDARG ;
      }


      //----------------------------------------------------------------------------
      /// <summary>
      /// 	<para>Virtual mode browse handling.</para>
      /// 	<para>Called only from the Professional edition generic server.</para>
      /// 	<para>
      /// 		<para>OPC DA V3 Browse method to browse the specified branch. This method is
      ///         called when the server is configured for virtual mode browsing and a client
      ///         calls the OPC DA V3 browse function. This method handles only the item browsing
      ///         part of the OPC DA browse function. The GetItemProperties method is called to
      ///         get the item's properties information.</para>
      /// 	</para>
      /// </summary>
      /// <param name="branch">The fully qualified name of the branch to be browsed</param>
      /// <param name="maxElements">The method must not return any more elements than this value. 
      /// If the server supports Continuation Points, then the Server may return a Continuation Point at a 
      /// value less than dwMaxElementsReturned. If the server does not support Continuation Points, 
      /// and more than dwMaxElementsReturned are available, then the Server shall return the 
      /// first dwMaxElementsReturned elements and set the pbMoreElements parameter to TRUE. 
      /// If dwMaxElementsReturned is 0 then there is no client side restriction on the number of 
      /// returned elements.</param>
      /// <param name="browseType">An OPCV3BrowseType enumeration value {All, Branch, Item} specifying which 
      /// subset of browse elements to return.</param>
      /// <param name="nameFilter">A wildcard string that conforms to the Visual Basic LIKE operator, 
      /// which will be used to filter Element names. A NUL String implies no filter.</param>
      /// <param name="vendorFilter">A server specific filter string. This is entirely free format and may 
      /// be entered by the user via an EDIT field. A pointer to a NUL string indicates no filtering.</param>
      /// <param name="continuationPoint">If this is a secondary call to Browse, the previous call might have 
      /// returned a Continuation Point where the Browse can restart from. Clients must pass a NUL string in 
      /// the initial call to Browse. This is an opaque value, which the server creates. 
      /// A Continuation Point will be returned if a Server does support Continuation Point, and the reply is 
      /// larger than dwMaxElementsReturned. The Continuation Point will allow the Client to resume the Browse 
      /// from the previous completion point.<br/>
      /// The continuationPoint is also used to indicate if there are more elements than the maxElements parameter 
      /// allows to be returned in this call. If there are more elements then continuationPoint must be set to NULL.
      /// Otherwise it is set to "" (an empty string) if exactly all elements could be returned or to an 
      /// implementation specific string for an actual continuation point.</param>
      /// <param name="numNodes">OUT: Number of nodes returned</param>
      /// <param name="nodes">
      /// OUT: Array of BrowseNodeInfo objects with the information of the browsed
      /// nodes.</param>
      /// <returns>HRESULT error/success code</returns>
      public int Browse( string branch, int maxElements, int browseType, string nameFilter, string vendorFilter,
         ref string continuationPoint, out int numNodes, out BrowseNodeInfo[] nodes  )
      {
         // not supported in this default implementation
         nodes = new BrowseNodeInfo[0] ;
         numNodes = nodes.Length ;
         return HRESULTS.S_FALSE ;
      }


      //-----------------------------------------------------------------------------------------
      /// <summary>
      /// 	<para>Virtual mode browse handling.</para>
      /// 	<para>Called only from the Professional edition generic server.</para>
      /// 	<para>Returns the the values of the requested properties for one item. This method
      ///     is called in virtual browse mode for items that are not yet added to the generic
      ///     cache.</para>
      /// </summary>
      /// <returns>
      /// HRESULT success/error code. Returns S_OK if all requested properties are
      /// successfully returned, S_FALSE if an error is returned for one or more properties. If
      /// an error is returned then the out parameters are null (Nothing)
      /// </returns>
      /// <param name="itemID">The fully qualified item ID.</param>
      /// <param name="returnValues">The property value must be returned additionally.</param>
      /// <param name="returnAll">All properties defined for this item have to be returned.
      /// The numProp and propertyIDs arguments are undefined in this case.</param>
      /// <param name="numProp">The number of requested properties. Ignore this argument if returnAll is true.</param>
      /// <param name="propertyIDs">
      /// REF: Array with the IDs of the properties to be returned. If returnAll is true
      /// then this argument should be ignored and used as an out parameter to return an array
      /// with the IDs of the returned properties.
      /// </param>
      /// <param name="propertyNames">OUT: Array with the name of each returned property. 
      /// This is used only in the XML DA server.</param>
      /// <param name="propertyDescr">OUT: Array with the description of each returned property.</param>
      /// <param name="Values">
      /// OUT: Array with the Value of each returned property. Only if returnValues is
      /// true. Otherwise null is returned.</param>
      /// <param name="errors">OUT: Array with the HRESULT code of each returned property.</param>
      /// <returns>HRESULT error/success code</returns>
      public int GetItemProperties( string itemID, bool returnValues, bool returnAll, ref int numProp, 
                                    ref int[] propertyIDs,  out string[] propertyNames, out string[] propertyDescr, 
                                    out Object[] Values, out int[]errors  )
      {
         // no items in this default implementation
         Values = null ;
         propertyNames = null;
         propertyDescr = null;
         errors = null ;
         return HRESULTS.OPC_E_UNKNOWNITEMID ;
      }

      internal static string[] NameStandardProperties = {  "",
                                                           "dataType",                 //1
                                                           "value",                    //2
                                                           "quality",                  //3
                                                           "timestamp",                //4
                                                           "accessRights",             //5
                                                           "scanRate",                 //6
                                                           "euType",                   //7
                                                           "euInfo"    };              //8

      internal static string[] DescrStandardProperties = {  "",
                                                          "Item Canonical DataType",   //1
                                                          "Item Value",                //2
                                                          "Item Quality",              //3
                                                          "Item Timestamp",            //4
                                                          "Item Access Rights",        //5
                                                          "Server Scan Rate",          //6
                                                          "EU Type",                   //7
                                                          "EU Info"    };              //8  string[] for euType enumerated



//
//
//      //==========================================================================================================
//      // AE Support
//
//
//      /// <summary>
//      /// Create an Alarms&Events Server instance.
//      /// </summary>
//      /// <param name="brw">Server object is returned to the generic server</param>
//      /// <returns>S_OK if the server object could be created.</returns>
//      public AlarmEventServer CreateEventServer() 
//      {
//         return new AlarmEventServer();
//      }

   }


   
   //==============================================================================================
   // Callback delegates
   // DON'T USE DIRECTLY. Use the methods in the class GenericServer instead.
   // ----------------------------------------------------------------------------
   /// <summary>
   /// Generic server callback to add a new item to the server's address space.
   /// </summary>
   public delegate int AddItem( int AppItemHandle, string ItemId, int AccessRights, 
   object InitValue, short quality, DateTime timestamp, int scanRate );

   /// <summary>
   /// Generic server callback to remove an unreferenced item from the server's address space.
   /// </summary>
   public delegate int DeleteItem( int AppItemHandle );

   /// <summary>
   /// Generic server callback to request a shout down.
   /// </summary>
   public delegate void ShutDownRequest( string reason );

   /// <summary>
   /// Generic server callback to change the server state.
   /// </summary>
   public delegate void SetServerState( int state );

   /// <summary>
   /// Generic server callback to change an item value.
   /// </summary>
   public delegate int SetItemValue( int AppItemHandle, object newValue, short quality, DateTime timestamp );

   /// <summary>
   /// Generic server callback to get a list of item that need to be refreshed.
   /// </summary>
   public delegate int GetRefreshNeed( int mode, int maxAge, out int numHandles, out IntPtr AppItemHandles );

   /// <summary>
   /// Generic server callback to get information about the current server status/usage.
   /// </summary>
   public delegate int GetServerInfo( out int state, out int instanceCount, out IntPtr groupCount );

   /// <summary>
   /// Generic server callback to get inforamtion about one item.
   /// </summary>
   public delegate int GetItemInfo( string itemID, out int GroupLinkCounter, out int GroupActiveLinkCounter,
   out int AppHandle, out int ScanRate, out int SamplingRate, out System.Type DataType );

   /// <summary>
   /// Generic server callback to get information about one or all server instances.
   /// There is a server instance for each client connection.
   /// </summary>
   public delegate int GetServerInstanceInfo(int handle, out int num, out IntPtr info );


}

