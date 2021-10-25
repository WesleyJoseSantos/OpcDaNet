//============================================================================
// DANSrv Customizable OPC DA Server
// ---------------------------------
//
// Generic OPC server part interface definitions.
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
//
// Copyright (C) 2003-2010 Advosol Inc.    (www.advosol.com)
// All rights reserved.
//-------------------------------------------------------------------------
using System;
using System.Xml;

namespace NSPlugin
{

     //***************************************************  SAMPLE CODE
   //============================================
   // 
   public class ConfigData
   {
      public ItemData[]    Items ;

      //---------------------------------------- constructor
      public ConfigData( int numItems )
      {
         Items = new ItemData[ numItems ] ;
      }

      //----------------------------------- construct from simple item definition
      public ConfigData( ItemDef[] itms )
      {
         Items = new ItemData[ itms.Length ] ;
         for( int i = 0 ; i<itms.Length ; ++i )
         {
            Items[i] = this.NewItem( itms[i].ID, i, OPCAccess.READWRITEABLE, itms[i].Value, OPCQuality.GOOD );
         }
      }


      //-----------------------------------------------------------------------------------
      // create a new item definition that can be assigned to an element of the 
      // item definition array
      public ItemData NewItem( string Name, int appHandle, OPCAccess aMode, object val, OPCQuality qual )
      {
         ItemData ni = new ItemData();
         ni.itemDefs = new ConfigDefs();
         ni.name = Name ;
         ni.handle = appHandle ;
         ni.itemDefs.accRight = aMode;
         ni.Value = val ;
         ni.Quality = qual ;
         ni.Timestamp = DateTime.Now ;
         return ni ;
      }


      //-----------------------------------------------------------------------------------
      // find the array index of the item with the specified fully qualified name
      public int findIndexOf( string name )
      {
         for( int i=0 ; i<Items.Length ; ++i )
            if( Items[i].name == name )
               return i ;
         return -1 ;
      }
   }



   //==================================================================
   // Simple Item and value definition
   public class ItemDef 
   {
      public string ID ;
      public object Value;

      public ItemDef( string id, object val )
      {
         ID = id ;
         Value = val ;
      }
   }


   //============================================
   // sample item definition class
   public class ItemData : ItemElement
   {
      public int           err ;        // error creating or processing the item
      public OPCQuality    Quality ;
      public DateTime      Timestamp ;
      //----------------- if the DA server is combined with an AE server
      //internal EventSource   AESource ;   // associated event source
      // add application specific data here
      public bool          isCreatedInServer;
   }

   //---------------------------------------------
   // item definition class as used in the ConfigBuilder class and Tool
   public class ItemElement
   {
      public ItemElement()                // needed for XML serializer
      {}
      public string        name ;         // fully qualified name (simple name within ConfigBuilder tool)
      public int           handle ;       // unique item handle
      public ConfigDefs    itemDefs ;     // configuration definitions
      public object        Value ;        // initial or current value
   }


   //-----------------------------------------------------------
   // Definitions for one item
   // The definiton is hierarchical. The branch level definitions are used
   // when there is no item level definition specified.
   public class ConfigDefs
   {
      public ConfigDefs()   // needed for XML serializer
      {}
      public bool          activeDef ;

      public OPCAccess     accRight ;
      public bool          accRightSpecified;
      public XmlQualifiedName      dataType ;
      public bool          dataTypeSpecified ;
      public PropertyDef[] properties ;
      public OPCQuality    quality ;
      public bool          qualitySpecified ;
      public SignalType    signalType ;
      public bool          signalTypeSpecified ;
      public int           scanRate ;
      public bool          scanRateSpecified ;
      public string        deviceID ;
      public bool          deviceIDSpecified ;
      public string        deviceAddr ;
      public bool          deviceAddrSpecified ;
      public string        deviceSubAddr ;
      public bool          deviceSubAddrSpecified ;
      public string        user1 ;
      public bool          user1Specified ;
      public string        user2 ;
      public bool          user2Specified ;
   }

   // Driver internal signal (item) types
   public enum SignalType  : short 
   {
      INP = 1,
      OUT = 2,
      INOUT = 3,
      INTERN = 7
   }

      
   //--------------------------------------------------------
   // Item Property Definition
   public class PropertyDef
   {
      public int     id;
      public string  name ;
      public XmlQualifiedName    dataType ;
      public string  descr ;
      public object  val ;

      public PropertyDef( int ID, string Name, string Descr, object Val )
      {
         id = ID;
         name = Name;
         descr = Descr;
         val = Val;
      }
   }
   //*************************************** END SAMPLE CODE
    

}
