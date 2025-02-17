using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkSpace.Models;
using System.Data;
using Microsoft.AspNetCore.Hosting;
using WorkSpace.Library;
using System.IO;
using System.Xml;
using System.Net;
//using Microsoft.AspNetCore.Hosting.Internal;
using SPACE_GATE.Class;
using System.IO.Compression;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Reflection;
using RestSharp;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Xml.Linq;
using System.Collections;

namespace WorkSpace.Controllers
{
 
    [Route("WS/V1")]
    public class WSController : Controller
    {
        String _Connection = "";
        String _OfficeSpaceId = "";
        String _DatabaseName = "";
        //teeteteteteee
        static NextwaverDB.NColumns NCS = new NextwaverDB.NColumns();
        static NextwaverDB.NWheres NWS = new NextwaverDB.NWheres();
        static XmlDocument xDoc = new XmlDocument();
        XmlDocument xDocBookClassLast = new XmlDocument();
        XmlDocument xDocBookLast = new XmlDocument();
        XmlDocument xDocPageLast = new XmlDocument();
        XmlDocument xDocSubPageLast = new XmlDocument();
        BookDetails bd = new BookDetails();
        BookClassLast bcl = new BookClassLast();
        BookLast bl = new BookLast();
        PageLast pl = new PageLast();
        SubPageLast spl = new SubPageLast();
        String EncryptKey = Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("EncryptKey").Value;

        String UserName;
        String ErrorMSG;
        #region Property    

        private String _ErrorMSG { get; set; }

        private String _UserName { get; set; }

        public IPathProvider _PathProvider { get; set; }
        #endregion

        #region Constructor

        public WSController(IPathProvider PathProvider)
        {
            _PathProvider = PathProvider;
 
        }
 
        #endregion

        #region Method (Utility)

        private String GetIP()
        {
            IPHostEntry host;

            String localIP = "?";

            host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily.ToString() == "InterNetwork")
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }

        private String GetPathStore()
        {
            return _PathProvider.MapPath("CV");
        }

        private void SetLog(String Command, String OfficeSpaceId, String ObjectId, String ItemId, String strxml, String ColumnData, String WhereData, String sErrorMSG)
        {
            if (_UserName == "")
                return;

            String Folder = _PathProvider.MapPath("Log/" + OfficeSpaceId);

            if (!Directory.Exists(Folder))
                Directory.CreateDirectory(Folder);

            Folder = Folder + "/" + ObjectId;

            if (!Directory.Exists(Folder))
                Directory.CreateDirectory(Folder);

            Folder = Folder + "/" + ItemId;

            if (!Directory.Exists(Folder))
                Directory.CreateDirectory(Folder);

            Folder = Folder + "/" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year;

            if (!Directory.Exists(Folder))
                Directory.CreateDirectory(Folder);

            Folder = Folder + "/" + _UserName;

            if (!Directory.Exists(Folder))
                Directory.CreateDirectory(Folder);

            String sData = @"
<Log>
<UserName>@UserName</UserName>
<ID>@ID</ID>
<Command>@Command</Command>
<OfficeSpaceId>@OfficeSpaceId</OfficeSpaceId>
<ObjectId>@ObjectId</ObjectId>
<ItemId>@ItemId</ItemId>
<ErrorMessage>@ErrorMessage</ErrorMessage>
<Data><!--@Data--></Data>
<ColumnData>@ColumnData</ColumnData>
<WhereData>@WhereData</WhereData>
</Log>";

            String ID = DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second + "-" + DateTime.Now.Millisecond;

            sData = sData.Replace("@ID", ID);
            sData = sData.Replace("@Command", Command);
            sData = sData.Replace("@OfficeSpaceId", OfficeSpaceId);
            sData = sData.Replace("@ObjectId", ObjectId);
            sData = sData.Replace("@OfficeSpaceId", OfficeSpaceId);
            sData = sData.Replace("@ItemId", ItemId);
            sData = sData.Replace("@Data", strxml);
            sData = sData.Replace("@ColumnData", ColumnData);
            sData = sData.Replace("@WhereData", WhereData);
            sData = sData.Replace("@ErrorMessage", sErrorMSG);
            sData = sData.Replace("@UserName", _UserName);

            String LogType = "E";

            if (sErrorMSG == "")
                LogType = "N";

            XmlDocument xDoc = new XmlDocument();

            xDoc.LoadXml(sData);

            xDoc.Save(Folder + "/" + LogType + "-" + ID + ".xml");
        }

        private ReturnStringList SetReturnStringList(String[] StringList)
        {
            ReturnStringList returnStringList = new ReturnStringList();

            foreach (String StringItem in StringList)
            {
                returnStringList.DataList.Add(new ReturnString(StringItem));
            }

            return returnStringList;
        }

        private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }
            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }
        #endregion

        #region Method (Service)

        [HttpGet("GetError")]
        public IActionResult GetError()
        {
            return Ok(SetReturnStringList(new String[] { "Error", _ErrorMSG }));
        }

        [HttpPost("SetPath")]
        public IActionResult SetPath(String Password, String PathStore)
        {
            if (Password != "Nextwaver.net")
                return Ok(new ReturnBoolean(false));

            XmlDocument xConfig = new XmlDocument();
            xConfig.Load(_PathProvider.MapPath("Config.xml"));

            xConfig.SelectSingleNode("//Config[@ID='Store']").Attributes["Value"].Value = PathStore;

            xConfig.Save(_PathProvider.MapPath("Config.xml"));

            return Ok(new ReturnBoolean(true));
        }

        [HttpPost("SetWorkspaceConfig")]
        public IActionResult SetWorkspaceConfig(String Password, String strWorkspace)
        {
            if (Password != "Nextwaver.net")
                return Ok(new ReturnBoolean(false));

            XmlDocument xConfig = new XmlDocument();

            xConfig.LoadXml(strWorkspace);

            xConfig.Save(_PathProvider.MapPath("WorkSpace.xml"));

            return Ok(new ReturnBoolean(true));
        }

        [HttpGet("GetDocumentByVersion")]
        public IActionResult GetDocumentByVersion(String Connection, String OfficeSpaceId, String DatabaseName, String TableName, String ItemId, String Version)
        {
            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง", "" }));
            }

            XmlDocument xFileSystem = new XmlDocument();

            xFileSystem.Load(_PathProvider.MapPath("FileSystem.xml"));

            String PathStore = GetPathStore();

            Int32 iID = Int32.Parse(ItemId);

            String ItemIdFolder = Gobals.Methods.GenItemFile(iID);

            String FolderId = xFileSystem.SelectSingleNode("//Item[@Min<=" + Int32.Parse(ItemId) + "][@Max>=" + Int32.Parse(ItemId) + "]").Attributes["ID"].Value;

            String ObjectId = "DB-" + DatabaseName + "$TB-" + TableName + "$DOC";

            Int32 iVersion = Int32.Parse(Version);

            String FolderVersion = xFileSystem.SelectSingleNode("//Item[@Min<=" + Int32.Parse(Version) + "][@Max>=" + Int32.Parse(Version) + "]").Attributes["ID"].Value;

            String RootPath = PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + FolderVersion;


            String PathDocLastVersion = RootPath + @"/" + Gobals.Methods.GenItemFile(1) + ".xml";

            String DocumentVersion = System.IO.File.ReadAllText(PathDocLastVersion);

            for (Int32 i = 2; i <= iVersion; i++)
            {
                String PathTemp = RootPath + @"/" + Gobals.Methods.GenItemFile(i) + ".xml";

                String Diff = System.IO.File.ReadAllText(PathTemp);

                String newVersionDocument = "";

                Boolean bError;

                String MsgError;

                Gobals.ControlVersion.PatchXML(DocumentVersion, Diff, _PathProvider.MapPath("Temp"), out newVersionDocument, out bError, out MsgError);

                System.Threading.Thread.Sleep(100);

                DocumentVersion = newVersionDocument;
            }

            return Ok(SetReturnStringList(new String[] { "", "", DocumentVersion }));
        }

        [HttpGet("GetDocumentVersion")]
        public IActionResult GetDocumentVersion(String Connection, String OfficeSpaceId, String DatabaseName, String TableName, String ItemId)
        {
            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง", "" }));
            }

            XmlDocument xFileSystem = new XmlDocument();

            xFileSystem.Load(_PathProvider.MapPath("FileSystem.xml"));

            String PathStore = GetPathStore();

            Int32 iID = Int32.Parse(ItemId);

            String ItemIdFolder = Gobals.Methods.GenItemFile(iID);

            String FolderId = xFileSystem.SelectSingleNode("//Item[@Min<=" + Int32.Parse(ItemId) + "][@Max>=" + Int32.Parse(ItemId) + "]").Attributes["ID"].Value;

            String ObjectId = "DB-" + DatabaseName + "$TB-" + TableName + "$DOC";

            String Version;

            try
            {
                String[] DirList = Directory.GetDirectories(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder);

                String LastVersionFolder = Gobals.Methods.GenFolderId(DirList.Length);

                String[] filList = Directory.GetFiles(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + LastVersionFolder);

                Version = "" + (((DirList.Length - 1) * 2000) + (filList.Length));
            }
            catch
            {
                Version = "0";
            }

            return Ok(SetReturnStringList(new String[] { "", "", Version }));
        }

        [HttpPost("CreateOfficeSpace")]
        public IActionResult CreateOfficeSpace(String Connection, String OfficeSpaceId)
        {
            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" }));
            }

            String strPath = _PathProvider.MapPath("Store/" + OfficeSpaceId);

            if (!Directory.Exists(strPath))
            {
                return Ok(SetReturnStringList(new String[] { "OK", "สร้าง OfficeSpace เรียบร้อยแล้ว" }));
            }
            else
            {
                return Ok(SetReturnStringList(new String[] { "Error", "มี OfficeSpace นี้อยู่แล้วในระบบ" }));
            }
        }

        [HttpPost("SaveDocumentNoSent")]
        public IActionResult SaveDocumentNoSent(String OfficeSpaceId, String ObjectId, String ItemId, String strDocument)
        {
            XmlDocument xFileSystem = new XmlDocument();

            xFileSystem.Load(_PathProvider.MapPath("FileSystem.xml"));

            String PathStore = GetPathStore();

            Int32 iID = Int32.Parse(ItemId);

            String ItemIdFolder = Gobals.Methods.GenItemFile(iID);

            String FolderId = xFileSystem.SelectSingleNode("//Item[@Min<=" + Int32.Parse(ItemId) + "][@Max>=" + Int32.Parse(ItemId) + "]").Attributes["ID"].Value;

            String Version;

            try
            {
                String[] DirList = Directory.GetDirectories(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder);

                String LastVersionFolder = Gobals.Methods.GenFolderId(DirList.Length);

                String[] filList = Directory.GetFiles(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + LastVersionFolder);

                Version = "" + (((DirList.Length - 1) * 2000) + (filList.Length + 1));

            }
            catch
            {
                Version = "1";
            }

            Gobals.Sockets.TCP_Client TCPC = new Gobals.Sockets.TCP_Client();

            String Server_IP = GetIP();

            String FolderVersion = xFileSystem.SelectSingleNode("//Item[@Min<=" + Int32.Parse(Version) + "][@Max>=" + Int32.Parse(Version) + "]").Attributes["ID"].Value;

            String FileVersionName = Gobals.Methods.GenItemFile(Int32.Parse(Version));

            //เริ่มสร้าง ROOT PATH
            if (!Directory.Exists(PathStore))
                Directory.CreateDirectory(PathStore);

            if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId))
                Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId);

            if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId))
                Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId);
            //จบการสร้าง ROOT PATH

            //เริ่มสร้าง Version Control
            if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV"))
                Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV");

            if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV"))
                Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV");
            //จบการสร้าง Version Control

            //เริ่มสร้าง Folder ID
            if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV\" + FolderId))
                Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV\" + FolderId);

            if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId))
                Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId);
            //จบการสร้าง Folder ID

            //เริ่มสร้าง Item Folder ID      
            if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder))
                Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder);
            //จบการสร้าง Item Folder ID

            //เริ่มสร้าง Item Folder Version ID       
            if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + FolderVersion))
                Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + FolderVersion);
            //จบการสร้าง Item Folder Version ID

            String SaveFileLastVersion = PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV\" + FolderId + @"\" + ItemIdFolder + ".xml";

            String SaveFileControlVersion = PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + FolderVersion + @"\" + FileVersionName + ".xml";

            if (Version != "1")
            {
                String Document_LastVersion = System.IO.File.ReadAllText(SaveFileLastVersion);

                Boolean bError = false;

                String outDocument = "", MsgError = "";

                Gobals.ControlVersion.PatchXML(Document_LastVersion, strDocument, _PathProvider.MapPath("Temp"), out outDocument, out bError, out MsgError);

                Gobals.Methods.SaveFile(SaveFileLastVersion, outDocument);

                Gobals.Methods.SaveFile(SaveFileControlVersion, strDocument);

                SetLog("WorkSpaceVer." + Version, OfficeSpaceId, ObjectId, ItemId, strDocument, "", "", "");

                SetLog("LastVersion" + Version, OfficeSpaceId, ObjectId, ItemId, Document_LastVersion, "", "", "");

                return Ok(SetReturnStringList(new String[] { "", "", Version }));
            }
            else
            {
                SetLog("WorkSpaceVer." + Version, OfficeSpaceId, ObjectId, ItemId, strDocument, "", "", "");

                //บันทึกข้อมูล Version สุดท้าย
                XmlDocument xTempp = new XmlDocument();

                xTempp.LoadXml(strDocument);

                xTempp.Save(SaveFileLastVersion);
                // จบการบันทึก        

                //บันทึกข้อมูล Control Version สุดท้าย
                xTempp.Save(SaveFileControlVersion);
                // จบการบันทึก

                return Ok(SetReturnStringList(new String[] { "", "", Version }));
            }
        }

        private ReturnStringList _SaveDocument(String OfficeSpaceId, String ObjectId, String ItemId, String strDocument)
        {
            XmlDocument xFileSystem = new XmlDocument();

            xFileSystem.Load(_PathProvider.MapPath("FileSystem.xml"));

            String PathStore = GetPathStore();

            Int32 iID = Int32.Parse(ItemId);

            String ItemIdFolder = Gobals.Methods.GenItemFile(iID);

            String FolderId = xFileSystem.SelectSingleNode("//Item[@Min<=" + Int32.Parse(ItemId) + "][@Max>=" + Int32.Parse(ItemId) + "]").Attributes["ID"].Value;

            String Version;

            try
            {
                String[] DirList = Directory.GetDirectories(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder);

                String LastVersionFolder = Gobals.Methods.GenFolderId(DirList.Length);

                String[] filList = Directory.GetFiles(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + LastVersionFolder);

                Version = "" + (((DirList.Length - 1) * 2000) + (filList.Length + 1));

            }
            catch
            {
                Version = "1";
            }

            Gobals.Sockets.TCP_Client TCPC = new Gobals.Sockets.TCP_Client();

            String Server_IP = GetIP();

            String FolderVersion = xFileSystem.SelectSingleNode("//Item[@Min<=" + Int32.Parse(Version) + "][@Max>=" + Int32.Parse(Version) + "]").Attributes["ID"].Value;

            String FileVersionName = Gobals.Methods.GenItemFile(Int32.Parse(Version));

            //เริ่มสร้าง ROOT PATH
            if (!Directory.Exists(PathStore))
                Directory.CreateDirectory(PathStore);

            if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId))
                Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId);

            if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId))
                Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId);
            //จบการสร้าง ROOT PATH

            //เริ่มสร้าง Version Control
            if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV"))
                Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV");

            if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV"))
                Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV");
            //จบการสร้าง Version Control

            //เริ่มสร้าง Folder ID
            if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV\" + FolderId))
                Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV\" + FolderId);

            if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId))
                Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId);
            //จบการสร้าง Folder ID

            //เริ่มสร้าง Item Folder ID      
            if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder))
                Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder);
            //จบการสร้าง Item Folder ID

            //เริ่มสร้าง Item Folder Version ID       
            if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + FolderVersion))
                Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + FolderVersion);
            //จบการสร้าง Item Folder Version ID

            String SaveFileLastVersion = PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\LV\" + FolderId + @"\" + ItemIdFolder + ".xml";

            String SaveFileControlVersion = PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + FolderVersion + @"\" + FileVersionName + ".xml";

            if (Version != "1")
            {
                String Document_LastVersion = System.IO.File.ReadAllText(SaveFileLastVersion);

                String docHas, strDIff = "", msgError;

                Boolean bError = false;

                Gobals.ControlVersion.CreateDiff(Document_LastVersion, strDocument, _PathProvider.MapPath("Temp"), out docHas, out strDIff, out bError, out msgError);

                if (strDIff == "")
                {
                    return SetReturnStringList(new String[] { "Error", "ไม่มีการแก้ไขข้อมูล", "" });
                }

                String strTemp = "<?xml version=\"1.0\" encoding=\"windows-874\"?><xd:xmldiff version=\"1.0\" srcDocHash=\"" + docHas + "\" options=\"None\" fragments=\"no\" xmlns:xd=\"http://schemas.microsoft.com/xmltools/2002/xmldiff\" />";

                strTemp = strTemp.Replace(" ", "");

                if (strDIff.Replace(" ", "") == strTemp)
                {
                    return SetReturnStringList(new String[] { "Error", "ไม่มีการแก้ไขข้อมูล", "" });
                }

                XmlDocument xTempp = new XmlDocument();

                xTempp.LoadXml(strDocument);

                xTempp.Save(SaveFileLastVersion);

                Gobals.Methods.SaveFile(SaveFileControlVersion, strDIff);

                return SetReturnStringList(new String[] { "", "", Version });
            }
            else
            {
                //บันทึกข้อมูล Version สุดท้าย
                XmlDocument xTempp = new XmlDocument();

                xTempp.LoadXml(strDocument);

                xTempp.Save(SaveFileLastVersion);
                // จบการบันทึก        

                //บันทึกข้อมูล Control Version สุดท้าย
                xTempp.Save(SaveFileControlVersion);
                // จบการบันทึก

                return SetReturnStringList(new String[] { "", "", Version });
            }
        }

        [HttpPost("SaveDocument")]
        public IActionResult SaveDocument(String OfficeSpaceId, String ObjectId, String ItemId, String strDocument)
        {
            return Ok(_SaveDocument(OfficeSpaceId, ObjectId, ItemId, strDocument));
        }

        [HttpGet("GetDocument_LastVersion")]
        public IActionResult GetDocument_LastVersion(String OfficeSpaceId, String ObjectId, String ItemId)
        {
            try
            {
                XmlDocument xFileSystem = new XmlDocument();

                xFileSystem.Load(_PathProvider.MapPath("FileSystem.xml"));

                String PathStore = GetPathStore();

                String FolderId = xFileSystem.SelectSingleNode("//Item[@Min<=" + int.Parse(ItemId) + "][@Max>=" + int.Parse(ItemId) + "]").Attributes["ID"].Value;

                Int32 iID = Int32.Parse(ItemId);

                String FileItem = Gobals.Methods.GenItemFile(iID);

                String Version;

                try
                {
                    String[] DirList = Directory.GetDirectories(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + FileItem);

                    String CV_FolderId = Gobals.Methods.GenFolderId(DirList.Length);

                    String[] filList = Directory.GetFiles(PathStore + @"\" + OfficeSpaceId + @"\" + ObjectId + @"\CV\" + FolderId + @"\" + FileItem + @"\" + CV_FolderId);

                    Version = "" + (((DirList.Length - 1) * 2000) + (filList.Length + 1));
                }
                catch (Exception ex)
                {
                    Version = "1";
                }

                String PathDocLastVersion = PathStore + @"/" + OfficeSpaceId + @"/" + ObjectId + @"/LV/" + FolderId + @"/" + FileItem + ".xml";

                String Document_LastVersion = System.IO.File.ReadAllText(PathDocLastVersion);

                SetLog("GetDocument_LastVersion", OfficeSpaceId, ObjectId, ItemId, Document_LastVersion, "", "", "");

                return Ok(SetReturnStringList(new String[] { "", "", Document_LastVersion }));
            }
            catch (Exception ex)
            {
                SetLog("GetDocument_LastVersion", OfficeSpaceId, ObjectId, ItemId, "", "", "", ex.Message);

                return Ok(SetReturnStringList(new String[] { "Error", ex.Message, "" }));
            }
        }

        [HttpGet("CreateDatabase")]
        public IActionResult CreateDatabase(String Connection, String OfficeSpaceId, String DatabaseName)
        {

            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" }));
            }

            String strPath = _PathProvider.MapPath("Store/" + OfficeSpaceId);

            if (!Directory.Exists(strPath))
            {
                return Ok(SetReturnStringList(new String[] { "Error", "ไม่พบ OfficeSpaceId ที่ระบุ" }));
            }

            NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, _PathProvider.MapPath(""));

            if (NDB.newDatabase(DatabaseName))
            {
                return Ok(SetReturnStringList(new String[] { "OK", "สร้างฐ้านข้อมูลสำเร็จ" }));
            }
            else
            {
                return Ok(SetReturnStringList(new String[] { "Error", NDB.ErrorMsg }));
            }
        }

        [HttpGet("CreateTable")]
        public IActionResult CreateTable(String Connection, String OfficeSpaceId, String DatabaseName, String TableName)
        {
            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" }));
            }

            String strPath = _PathProvider.MapPath("Store/" + OfficeSpaceId);

            if (!Directory.Exists(strPath))
            {
                return Ok(SetReturnStringList(new String[] { "Error", "ไม่พบ OfficeSpaceId ที่ระบุ" }));
            }

            NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, _PathProvider.MapPath(""));

            if (NDB.newTable(DatabaseName, TableName))
            {
                XmlDocument xDoc = new XmlDocument();

                xDoc.Load(NDB.OutputXmlFile);

                String ObjectId = "DB-" + DatabaseName + "$TB-" + TableName;

                try
                {
                    ReturnStringList returnStringList = _SaveDocument(OfficeSpaceId, ObjectId, "1", xDoc.OuterXml);
                }
                catch (Exception ex)
                {
                    SetLog("SaveDocument", OfficeSpaceId, ObjectId, "1", xDoc.OuterXml, "", "", ex.Message);
                }

                return Ok(SetReturnStringList(new String[] { "OK", "สร้างตารางสำเร็จ" }));
            }
            else
            {
                return Ok(SetReturnStringList(new String[] { "Error", NDB.ErrorMsg }));
            }
        }

        [HttpGet("CreateColumn")]
        public IActionResult CreateColumn(String Connection, String OfficeSpaceId, String DatabaseName, String TableName, String ColumnName, String Detail, NextwaverDB.NColumnType NCType)
        {
            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" }));
            }

            NCType = NextwaverDB.NColumnType.Varchar;
            String strPath = _PathProvider.MapPath("Store/" + OfficeSpaceId);

            if (!Directory.Exists(strPath))
            {
                return Ok(SetReturnStringList(new String[] { "Error", "ไม่พบ OfficeSpaceId ที่ระบุ" }));
            }

            NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, _PathProvider.MapPath(""));

            if (NDB.newColumn(DatabaseName, TableName, ColumnName, Detail, NCType))
            {
                NextwaverDB.NOutputXMLs NOPX_Update = NDB.NOPXMLS_Update;

                for (Int32 i = 0; i < NOPX_Update._Count; i++)
                {
                    NextwaverDB.NOutputXML NOPX = NOPX_Update.get(i);

                    String FID = NOPX.ObjectID;

                    XmlDocument xDoc = new XmlDocument();

                    xDoc.LoadXml(NOPX.strXML);

                    String ObjectId = "DB-" + DatabaseName + "$TB-" + TableName;

                    try
                    {
                        ReturnStringList returnStringList = _SaveDocument(OfficeSpaceId, ObjectId, FID, xDoc.OuterXml);
                    }
                    catch (Exception ex)
                    {
                        SetLog("SaveDocument", OfficeSpaceId, ObjectId, FID, xDoc.OuterXml, "", "", ex.Message);
                    }
                }

                return Ok(SetReturnStringList(new String[] { "OK", "สร้างคอลัมภ์สำเร็จ" }));
            }
            else
            {
                return Ok(SetReturnStringList(new String[] { "Error", NDB.ErrorMsg }));
            }
        }


        [HttpPost("InsertData")]
        public IActionResult InsertData(String Connection, String OfficeSpaceId, String DatabaseName, String TableName, String NColumns_String, String strDOC, String User)
        {
            _UserName = User;

            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                SetLog("InsertData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, "", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง");

                return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง", "", "", "", "" }));
            }

            NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, _PathProvider.MapPath(""));

            NextwaverDB.NColumns NCS = new NextwaverDB.NColumns();

            var file = Request.Form.Files[0];
            if (file.Length > 0)
            {
                var Stream = file.OpenReadStream();
                using (var streamReader = new StreamReader(Stream, Encoding.UTF8))
                {
                    strDOC = streamReader.ReadToEnd();
                }
            }
            else
            {
                strDOC = "";
            }

            NCS.strXML = strDOC;

            if (!NCS.ImportString(NColumns_String))
            {
                SetLog("InsertData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, "", NCS.ErrorMSG);

                return Ok(SetReturnStringList(new String[] { "Error", NCS.ErrorMSG, "", "", "", "" }));
            }

            try
            {
                if (NDB.insert(DatabaseName, TableName, NCS))
                {
                    String Version = "";

                    String VersionDoc = "";

                    XmlDocument xDoc = new XmlDocument();

                    xDoc.Load(NDB.OutputXmlFile);

                    String ObjectId = "DB-" + DatabaseName + "$TB-" + TableName;

                    try
                    {
                        ReturnStringList returnStringList = _SaveDocument(OfficeSpaceId, ObjectId, NDB.OutputFileID, xDoc.OuterXml);

                        if (returnStringList.DataList[0].Data == "")
                        {
                            Version = returnStringList.DataList[2].Data;
                        }
                        else
                        {
                            throw new Exception(returnStringList.DataList[1].Data);
                        }
                    }
                    catch (Exception ex)
                    {
                        SetLog("SaveDocument", OfficeSpaceId, ObjectId, NDB.OutputFileID, xDoc.OuterXml, "", "", ex.Message);
                    }

                    if (NCS.strXML != "")
                    {
                        xDoc = new XmlDocument();

                        xDoc.LoadXml(NCS.strXML);

                        ObjectId = "DB-" + DatabaseName + "$TB-" + TableName + "$DOC";

                        try
                        {
                            ReturnStringList returnStringList = _SaveDocument(OfficeSpaceId, ObjectId, NDB.OutputFileID, xDoc.OuterXml);

                            if (returnStringList.DataList[0].Data == "")
                            {
                                VersionDoc = returnStringList.DataList[2].Data;
                            }
                            else
                            {
                                throw new Exception(returnStringList.DataList[1].Data);
                            }
                        }
                        catch (Exception ex)
                        {
                            SetLog("SaveDocument", OfficeSpaceId, ObjectId, NDB.OutputFileID, xDoc.OuterXml, "", "", ex.Message);
                        }
                    }

                    SetLog("InsertData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, "", "");

                    try
                    {
                        Transform(Connection, OfficeSpaceId, DatabaseName, TableName, NDB.NewItemID, User, true);
                    }
                    catch { }

                    return Ok(SetReturnStringList(new String[] { "OK", "เพิ่มข้อมูลเรียบร้อยแล้ว", Version, VersionDoc, NDB.NewItemID, NDB.OutputFileID }));
                }
                else
                {
                    SetLog("InsertData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, "", NDB.ErrorMsg);

                    return Ok(SetReturnStringList(new String[] { "Error", NDB.ErrorMsg, "", "", "", "" }));
                }
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        [HttpPost("UpdateData")]
        public IActionResult UpdateData(String Connection, String OfficeSpaceId, String DatabaseName, String TableName, String NColumns_String, String NWheres_String, String strDOC, String User)
        {
            _UserName = User;

            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                SetLog("UpdateData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, NWheres_String, "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง");

                return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง", "", "" }));
            }


            var file = Request.Form.Files[0];
            if (file.Length > 0)
            {
                var Stream = file.OpenReadStream();
                using (var streamReader = new StreamReader(Stream, Encoding.UTF8))
                {
                    strDOC = streamReader.ReadToEnd();
                }
            }
            else
            {
                strDOC = "";
            }

            NextwaverDB.NColumns NCS = new NextwaverDB.NColumns();

            NCS.strXML = strDOC;

            if (!NCS.ImportString(NColumns_String))
            {
                SetLog("UpdateData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, NWheres_String, NCS.ErrorMSG);

                return Ok(SetReturnStringList(new String[] { "Error", NCS.ErrorMSG, "", "" }));
            }

            NextwaverDB.NWheres NWS = new NextwaverDB.NWheres();

            if (!NWS.ImportString(NWheres_String))
            {
                SetLog("UpdateData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, NWheres_String, NCS.ErrorMSG);

                return Ok(SetReturnStringList(new String[] { "Error", NWS.ErrorMSG, "", "" }));
            }

            NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, _PathProvider.MapPath(""));

            if (NDB.update(DatabaseName, TableName, NCS, NWS))
            {
                String Version = "", VersionDoc = "";

                NextwaverDB.NOutputXMLs NOPX_Update = NDB.NOPXMLS_Update;

                for (Int32 i = 0; i < NOPX_Update._Count; i++)
                {
                    NextwaverDB.NOutputXML NOPX = NOPX_Update.get(i);

                    String FID = NOPX.ObjectID;

                    XmlDocument xDoc = new XmlDocument();

                    xDoc.LoadXml(NOPX.strXML);

                    String ObjectId = "DB-" + DatabaseName + "$TB-" + TableName;

                    try
                    {
                        ReturnStringList returnStringList = _SaveDocument(OfficeSpaceId, ObjectId, FID, xDoc.OuterXml);
                    }
                    catch (Exception ex)
                    {
                        SetLog("SaveDocument", OfficeSpaceId, ObjectId, FID, xDoc.OuterXml, "", "", ex.Message);
                    }
                }

                NextwaverDB.NOutputXMLs NOPX_Doc = NDB.NOPXMLS_Doc;

                for (Int32 i = 0; i < NOPX_Doc._Count; i++)
                {
                    NextwaverDB.NOutputXML NOPX = NOPX_Doc.get(i);

                    String FID = NOPX.ObjectID;

                    XmlDocument xDoc = new XmlDocument();

                    xDoc.LoadXml(NOPX.strXML);

                    String ObjectId = "DB-" + DatabaseName + "$TB-" + TableName + "$DOC";

                    try
                    {
                        ReturnStringList returnStringList = _SaveDocument(OfficeSpaceId, ObjectId, FID, xDoc.OuterXml);
                    }
                    catch (Exception ex)
                    {
                        SetLog("SaveDocument", OfficeSpaceId, ObjectId, FID, xDoc.OuterXml, "", "", ex.Message);
                    }
                }

                try
                {
                    Transform(Connection, OfficeSpaceId, DatabaseName, TableName, NWS.Get("ID").Value, User, true);
                }
                catch
                { }

                SetLog("UpdateData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, NWheres_String, "");

                return Ok(SetReturnStringList(new String[] { "OK", NDB.OutputMsg, Version, VersionDoc }));
            }
            else
            {
                SetLog("UpdateData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, NWheres_String, NDB.ErrorMsg);

                return Ok(SetReturnStringList(new String[] { "Error", NDB.ErrorMsg, "", "" }));
            }
        }

        [HttpGet("SelectByColumnAndWhere")]
        public IActionResult SelectByColumnAndWhere(String Connection, String OfficeSpaceId, String DatabaseName, String TableName, String NColumns_encrypt, String NWheres_encrypt, String User)
        {
            // String NWheres_String = new EncryptDecrypt.CryptorEngine().Decrypt(NWheres_encrypt, true);
            // String NColumns_String = new EncryptDecrypt.CryptorEngine().Decrypt(NColumns_encrypt, true);

            string NColumns_String = Decrypt(NColumns_encrypt);
            string NWheres_String = Decrypt(NWheres_encrypt);

            _UserName = User;


            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                SetLog("SelectByColumnAndWhere", OfficeSpaceId, DatabaseName, TableName, "", NColumns_String, NWheres_String, "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง");

                return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" }));
            }

            NextwaverDB.NColumns NCS = new NextwaverDB.NColumns();

            if (!NCS.ImportString(NColumns_String))
            {
                SetLog("SelectByColumnAndWhere", OfficeSpaceId, DatabaseName, TableName, "", NColumns_String, NWheres_String, "NColumns:" + NCS.ErrorMSG);

                return Ok(SetReturnStringList(new String[] { "Error", NCS.ErrorMSG }));
            }

            NextwaverDB.NWheres NWS = new NextwaverDB.NWheres();

            if (!NWS.ImportString(NWheres_String))
            {
                SetLog("SelectByColumnAndWhere", OfficeSpaceId, DatabaseName, TableName, "", NColumns_String, NWheres_String, "NWheres:" + NWS.ErrorMSG);

                return Ok(SetReturnStringList(new String[] { "Error", NWS.ErrorMSG }));
            }

            NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, _PathProvider.MapPath(""));

            DataTable dt = NDB.select(DatabaseName, TableName, NCS, NWS);

            if (dt == null)
            {
                SetLog("SelectByColumnAndWhere", OfficeSpaceId, DatabaseName, TableName, "", NColumns_String, NWheres_String, NDB.ErrorMsg);

                return Ok(SetReturnStringList(new String[] { "Error", NDB.ErrorMsg }));
            }
            else
            {
                SetLog("SelectByColumnAndWhere", OfficeSpaceId, DatabaseName, TableName, "", NColumns_String, NWheres_String, "");
            }

            return Ok(new ReturnDataTable(dt));
        }

        [HttpGet("SelectAllColumnByWhere")]
        public IActionResult SelectAllColumnByWhere(String Connection, String OfficeSpaceId, String DatabaseName, String TableName, String NWheres_String, String User)
        {
            _UserName = User;

            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                SetLog("SelectAllColumnByWhere", OfficeSpaceId, DatabaseName, TableName, "", "", NWheres_String, "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง");

                return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" }));
            }

            NextwaverDB.NWheres NWS = new NextwaverDB.NWheres();

            if (!NWS.ImportString(NWheres_String))
            {
                SetLog("SelectAllColumnByWhere", OfficeSpaceId, DatabaseName, TableName, "", "", NWheres_String, "NWheres:" + NWS.ErrorMSG);

                return Ok(SetReturnStringList(new String[] { "Error", NWS.ErrorMSG }));
            }

            NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, _PathProvider.MapPath(""));

            DataTable dt = NDB.select(DatabaseName, TableName, NWS);

            if (dt == null)
            {
                SetLog("SelectAllColumnByWhere", OfficeSpaceId, DatabaseName, TableName, "", "", NWheres_String, NDB.ErrorMsg);

                return Ok(SetReturnStringList(new String[] { "Error", NDB.ErrorMsg }));
            }
            else
            {
                SetLog("SelectAllColumnByWhere", OfficeSpaceId, DatabaseName, TableName, "", "", NWheres_String, "");
            }

            return Ok(new ReturnDataTable(dt));
        }

        [HttpGet("SelectAllByColumn")]
        public IActionResult SelectAllByColumn(String Connection, String OfficeSpaceId, String DatabaseName, String TableName, String NColumns_String, String User)
        {
            _UserName = User;

            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                SetLog("SelectAllByColumn", OfficeSpaceId, DatabaseName, TableName, "", NColumns_String, "", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง");

                return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" }));
            }

            NextwaverDB.NColumns NCS = new NextwaverDB.NColumns();

            if (!NCS.ImportString(NColumns_String))
            {
                SetLog("SelectAllByColumn", OfficeSpaceId, DatabaseName, TableName, "", NColumns_String, "", "NColumns:" + NCS.ErrorMSG);

                return Ok(SetReturnStringList(new String[] { "Error", NCS.ErrorMSG }));
            }

            NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, _PathProvider.MapPath(""));

            DataTable dt = NDB.select(DatabaseName, TableName, NCS);

            if (dt == null)
            {
                SetLog("SelectAllByColumn", OfficeSpaceId, DatabaseName, TableName, "", NColumns_String, "", NDB.ErrorMsg);

                return Ok(SetReturnStringList(new String[] { "Error", NDB.ErrorMsg }));
            }
            else
            {
                SetLog("SelectAllByColumn", OfficeSpaceId, DatabaseName, TableName, "", NColumns_String, "", "");
            }

            return Ok(new ReturnDataTable(dt));
        }

        [HttpGet("SelectAll")]
        public IActionResult SelectAll(String Connection, String OfficeSpaceId, String DatabaseName, String TableName, String User)
        {
            _UserName = User;
            if (Connection == null || OfficeSpaceId == null || DatabaseName == null || TableName == null || User == null)
            {
                return Ok(new ReturnString("Get Input:{Connection:" + Connection + ", OfficeSpaceId:" + OfficeSpaceId +
                   ", DatabaseName: " + DatabaseName + ", TableName: " + TableName + ", User: " + User + "}"));
            }
            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                SetLog("SelectAll", OfficeSpaceId, DatabaseName, TableName, "", "", "", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง");

                return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" }));
            }

            NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, _PathProvider.MapPath(""));

            DataTable dt = NDB.select(DatabaseName, TableName);

            if (dt == null)
            {
                SetLog("SelectAll", OfficeSpaceId, DatabaseName, TableName, "", "", "", NDB.ErrorMsg);

                return Ok(SetReturnStringList(new String[] { "Error", NDB.ErrorMsg }));
            }
            else
            {
                SetLog("SelectAll", OfficeSpaceId, DatabaseName, TableName, "", "", "", "");
            }

            return Ok(new ReturnDataTable(dt));
            //return Json(dt);
        }

        [HttpPost("UpdateTable")]
        public IActionResult UpdateTable(String Connection, String OfficeSpaceId, String ItemId, String DatabaseName, String TableName, String strData, String User)
        {
            _UserName = User;

            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                SetLog("updateTable", OfficeSpaceId, DatabaseName, TableName, strData, "", "", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง");

                return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" }));
            }

            String strPath = _PathProvider.MapPath("Store/" + OfficeSpaceId);

            if (!Directory.Exists(strPath))
            {
                SetLog("updateTable", OfficeSpaceId, DatabaseName, TableName, strData, "", "", "ไม่พบ OfficeSpaceId ที่ระบุ");

                return Ok(SetReturnStringList(new String[] { "Error", "ไม่พบ OfficeSpaceId ที่ระบุ" }));
            }

            try
            {
                strPath = strPath + "/database/" + DatabaseName;

                if (!Directory.Exists(strPath))
                    Directory.CreateDirectory(strPath);

                strPath = strPath + "/" + TableName;

                if (!Directory.Exists(strPath))
                    Directory.CreateDirectory(strPath);

                NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, _PathProvider.MapPath(""));

                String FileID = NDB.getFileID(Int32.Parse(ItemId));

                XmlDocument xTempp = new XmlDocument();

                xTempp.LoadXml(strData);

                xTempp.Save(strPath + "/" + FileID + ".xml");

                SetLog("updateTable", OfficeSpaceId, DatabaseName, TableName, strData, "", "", "");

                return Ok(SetReturnStringList(new String[] { "OK", "แก้ไขตารางเรียบร้อยแล้ว" }));
            }
            catch (Exception ex)
            {
                SetLog("updateTable", OfficeSpaceId, DatabaseName, TableName, strData, "", "", ex.Message);

                return Ok(SetReturnStringList(new String[] { "Error", "EXC:ITEM-" + ItemId + "_" + ex.Message }));
            }
        }

        private ReturnStringList _SelectLastDocument(String Connection, String OfficeSpaceId, String DatabaseName, String TableName, String ItemId, String User)
        {
            _UserName = User;

            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                SetLog("SelectLastDocument", OfficeSpaceId, DatabaseName, TableName, "", "", "", "[ID=" + ItemId + "]" + "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง");

                return SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง", "" });
            }

            NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, _PathProvider.MapPath(""));

            String OutputXML = "";

            if (NDB.selectLastDoc(DatabaseName, TableName, Int32.Parse(ItemId), out OutputXML))
            {
                SetLog("SelectLastDocument", OfficeSpaceId, DatabaseName, TableName, "", "", "", "");

                return SetReturnStringList(new String[] { "", "", OutputXML });
            }
            else
            {
                SetLog("SelectLastDocument", OfficeSpaceId, DatabaseName, TableName, "", "", "", "[ID=" + ItemId + "]" + NDB.ErrorMsg);

                return SetReturnStringList(new String[] { "Error", NDB.ErrorMsg, "" });
            }
        }

        [HttpGet("SelectLastDocument")]
        public IActionResult SelectLastDocument(String Connection, String OfficeSpaceId, String DatabaseName, String TableName, String ItemId, String User)
        {
            return Ok(_SelectLastDocument(Connection, OfficeSpaceId, DatabaseName, TableName, ItemId, User));
        }

        [HttpGet("GetRootCV")]
        public IActionResult GetRootCV(String Connection)
        {
            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" }));
            }

            return Ok(SetReturnStringList(Directory.GetDirectories(GetPathStore())));
        }

        [HttpGet("GetDirectoryInfo")]
        public IActionResult GetDirectoryInfo(String Connection, String Path)
        {
            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง", "" }));
            }

            DirectoryInfo DIF = new DirectoryInfo(Path);

            return Ok(new ReturnString(DIF.Name + "(" + DIF.LastWriteTime.Day + "/" + DIF.LastWriteTime.Month + "/" + DIF.LastWriteTime.Year + ")"));
        }

        [HttpGet("GetDirectoryList")]
        public IActionResult GetDirectoryList(String Connection, String Path)
        {
            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง", "" }));
            }

            return Ok(SetReturnStringList(Directory.GetDirectories(Path)));
        }

        [HttpGet("GetFileList")]
        public IActionResult GetFileList(String Connection, String Path)
        {
            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง", "" }));
            }

            return Ok(SetReturnStringList(Directory.GetFiles(Path)));
        }

        [HttpGet("GetFileName")]
        public IActionResult GetFileName(String Connection, String FilePath)
        {
            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง", "" }));
            }

            FileInfo FIF = new FileInfo(FilePath);

            return Ok(new ReturnString(FIF.Name));
        }

        [HttpPost("Transform")]
        public IActionResult Transform(String Connection, String OfficeSpaceId, String DatabaseName, String TableName, String ItemId, String User, Boolean isTransformDoc)
        {
            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                SetLog("Transform", OfficeSpaceId, DatabaseName, TableName, "" + ItemId, "", "", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง");

                return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง", "false" }));
            }

            XmlDocument xmlTemp = new XmlDocument();

            xmlTemp.Load(_PathProvider.MapPath("Config/Process.xml"));

            XmlNode Process = xmlTemp.SelectSingleNode("//Process[@OFSID='" + OfficeSpaceId + "'][@WSTable='" + TableName + "']");

            if (Process == null)
            {
                SetLog("Transform", OfficeSpaceId, DatabaseName, TableName, "" + ItemId, "", "", "ไม่พบคำสั่ง Transform");

                return Ok(SetReturnStringList(new String[] { "Error", "ไม่พบคำสั่ง Transform", "false" }));
            }

            SetLog("Transform", OfficeSpaceId, DatabaseName, TableName, ItemId.ToString(), "", "", "Start");

            String Sql_Delete = Process.SelectSingleNode("./Query/Delete").InnerText;

            String Sql_Insert = Process.SelectSingleNode("./Query/Insert").InnerText;

            XmlNodeList listColumn = Process.SelectNodes("./Columns/Column");

            String DBConnectionID = Process.Attributes["DBConnectionID"].Value;

            XmlNode nodeDBConnection = xmlTemp.SelectSingleNode("//Config/Connection/Item[@ID='" + DBConnectionID + "']");

            String DBServer = nodeDBConnection.Attributes["Server"].Value;

            String DBDatabase = nodeDBConnection.Attributes["Database"].Value;

            String DBLogin = nodeDBConnection.Attributes["Login"].Value;

            String DBPassword = nodeDBConnection.Attributes["Password"].Value;

            String ConnectoinString = "Data Source=" + DBServer + "; uid=" + DBLogin + "; pwd=" + DBPassword + "; Initial Catalog=" + DBDatabase + ";";

            NextwaverDB.NWheres NWS = new NextwaverDB.NWheres();

            NWS.Add(new NextwaverDB.NWhere("ID", ItemId.ToString()));



            String NWheres_String = NWS.ExportString();

            if (!NWS.ImportString(NWheres_String))
            {
                SetLog("Transform", OfficeSpaceId, DatabaseName, TableName, ItemId.ToString(), "", NWheres_String, "NWheres:" + NWS.ErrorMSG);

                return Ok(SetReturnStringList(new String[] { "Error", NWS.ErrorMSG }));
            }

            NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, _PathProvider.MapPath(""));

            DataTable dt = NDB.select(DatabaseName, TableName, NWS);

            if (dt == null)
            {
                SetLog("Transform", OfficeSpaceId, DatabaseName, TableName, ItemId.ToString(), "", NWheres_String, NDB.ErrorMsg);

                return Ok(SetReturnStringList(new String[] { "Error", NDB.ErrorMsg }));
            }
            else
            {
                SetLog("Transform", OfficeSpaceId, DatabaseName, TableName, ItemId.ToString(), "", NWheres_String, "");
            }


            List<String> Sql_List = new List<String>();

            for (Int32 k = 0; k < dt.Rows.Count; k++)
            {
                String TempInsert = Sql_Insert;

                String TempDelete = Sql_Delete;

                DataRow DR = dt.Rows[k];

                String strXML = "";

                for (Int32 j = 0; j < listColumn.Count; j++)
                {
                    String TempType = "" + listColumn[j].Attributes["Type"].Value;

                    String TempName = "" + listColumn[j].Attributes["Name"].Value;

                    String TempParameter = "" + listColumn[j].Attributes["Parameter"].Value;

                    String TempValue = "";

                    try
                    {
                        TempValue = "" + DR[TempName];
                    }
                    catch { }

                    TempDelete = TempDelete.Replace(TempParameter, TempValue);

                    switch (TempType)
                    {
                        case "STR":
                            TempInsert = TempInsert.Replace(TempParameter, TempValue);
                            break;

                        case "XML":
                            ReturnStringList returnStringList = _SelectLastDocument(Connection, OfficeSpaceId, DatabaseName, TableName, ItemId, User);

                            strXML = returnStringList.DataList[2].Data;

                            TempInsert = TempInsert.Replace(TempParameter, strXML);
                            break;
                    }
                }

                Sql_List.Add(TempDelete);

                Sql_List.Add(TempInsert);

                // Transform Document
                if (isTransformDoc)
                {
                    XmlNodeList listItemDocument = Process.SelectNodes("./Document/Items");


                    if (listItemDocument.Count != 0)
                    {
                        if (strXML == "")
                        {
                            ReturnStringList returnStringList = _SelectLastDocument(Connection, OfficeSpaceId, DatabaseName, TableName, ItemId, User);

                            strXML = returnStringList.DataList[2].Data;
                        }

                        XmlDocument xTemp = new XmlDocument();

                        xTemp.LoadXml(strXML);

                        for (Int32 j = 0; j < listItemDocument.Count; j++)
                        {
                            String ItmName = listItemDocument[j].Attributes["Name"].Value;

                            String ItmType = listItemDocument[j].Attributes["Type"].Value;

                            String ItmSql_Delete = listItemDocument[j].SelectSingleNode("./Query/Delete").InnerText;

                            String ItmSql_Insert = listItemDocument[j].SelectSingleNode("./Query/Insert").InnerText;

                            XmlNodeList ItmlistColumn = listItemDocument[j].SelectNodes("./Columns/Column");

                            XmlNode nodeRealData = xTemp.SelectSingleNode("//Items[@Name='" + ItmName + "']");

                            ItmSql_Delete = ItmSql_Delete.Replace("@ID@", "" + ItemId);

                            Sql_List.Add(ItmSql_Delete);

                            if (ItmType.ToUpper() == "FIX")
                            {
                                for (Int32 w = 0; w < ItmlistColumn.Count; w++)
                                {
                                    String TempName = "" + ItmlistColumn[w].Attributes["Name"].Value;

                                    String TempParameter = "" + ItmlistColumn[w].Attributes["Parameter"].Value;

                                    try
                                    {
                                        XmlNode nodeTEMPPP = nodeRealData.SelectSingleNode("./Item[@Name='" + TempName + "']");

                                        String TempValue = nodeTEMPPP.Attributes["Value"].Value;

                                        ItmSql_Insert = ItmSql_Insert.Replace(TempParameter, TempValue);
                                    }
                                    catch { }
                                }

                                ItmSql_Insert = ItmSql_Insert.Replace("@ID@", "" + ItemId);

                                Sql_List.Add(ItmSql_Insert);
                            }
                            else
                            {
                                XmlNode nodeMeans = nodeRealData.SelectSingleNode("./Means");

                                XmlNodeList listItmSub = nodeRealData.SelectNodes("./Item");

                                for (Int32 h = 0; h < listItmSub.Count; h++)
                                {
                                    String ItmSql_InsertSub = ItmSql_Insert;

                                    for (Int32 w = 0; w < ItmlistColumn.Count; w++)
                                    {
                                        String TempName = "" + ItmlistColumn[w].Attributes["Name"].Value;

                                        String TempParameter = "" + ItmlistColumn[w].Attributes["Parameter"].Value;

                                        try
                                        {
                                            String KeyID = nodeMeans.SelectSingleNode("./Mean[@Name='" + TempName + "']").Attributes["ID"].Value;

                                            String TempValue = listItmSub[h].Attributes[KeyID].Value;

                                            ItmSql_InsertSub = ItmSql_InsertSub.Replace(TempParameter, TempValue);
                                        }
                                        catch { }
                                    }

                                    try
                                    {
                                        XmlNodeList GobalColumns = listItemDocument[j].SelectNodes("./GobalColumns/Column");

                                        String GobalColumns_Name = listItemDocument[j].SelectSingleNode("./GobalColumns").Attributes["Name"].Value;

                                        XmlNode nodeRealGobalData = xTemp.SelectSingleNode("//Items[@Name='" + GobalColumns_Name + "']");

                                        for (Int32 w = 0; w < GobalColumns.Count; w++)
                                        {
                                            String TempName = "" + GobalColumns[w].Attributes["Name"].Value;

                                            String TempParameter = "" + GobalColumns[w].Attributes["Parameter"].Value;

                                            try
                                            {
                                                XmlNode nodeTEMPPP = nodeRealGobalData.SelectSingleNode("./Item[@Name='" + TempName + "']");

                                                String TempValue = nodeTEMPPP.Attributes["Value"].Value;

                                                ItmSql_InsertSub = ItmSql_InsertSub.Replace(TempParameter, TempValue);
                                            }
                                            catch { }
                                        }
                                    }
                                    catch { }

                                    ItmSql_InsertSub = ItmSql_InsertSub.Replace("@ID@", "" + ItemId);

                                    Sql_List.Add(ItmSql_InsertSub);
                                }
                            }
                        }
                    }
                }
            }

            ConnectServer cConn = new ConnectServer();

            if (cConn.Execute(Sql_List.ToArray(), ConnectoinString))
            {
                SetLog("Transform", OfficeSpaceId, DatabaseName, TableName, ItemId.ToString(), "", "", "");

                return Ok(SetReturnStringList(new String[] { "", "", "true" }));
            }
            else
            {
                SetLog("Transform", OfficeSpaceId, DatabaseName, TableName, ItemId.ToString(), "", "", cConn._ErrorLog);

                return Ok(SetReturnStringList(new String[] { "", "", "false" }));
            }
        }

        #endregion

        #region Spage Gate Service
        [HttpPost("CreateSpaceGate")]
        public IActionResult CreateSpaceGate(String Connection, String OfficeSpaceId, String SpaceGateIP, String SpaceGateDomain)
        {
            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" }));
            }

            String strPath = _PathProvider.MapPath("Store/" + OfficeSpaceId);

            if (!Directory.Exists(strPath))
            {
                return Ok(SetReturnStringList(new String[] { "Error", "ไม่พบ OfficeSpaceId ที่ระบุ" }));
            }

            String targetDirectory = _PathProvider.MapPath(@"Store\" + OfficeSpaceId + @"\database");
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            if (!Directory.Exists(targetDirectory))
                Directory.CreateDirectory(targetDirectory);

            String sourceDirectory = _PathProvider.MapPath("TempDocSpaceGate");
            if (!Directory.Exists(_PathProvider.MapPath("TempDocSpaceGate")))
            {
                return Ok(SetReturnStringList(new String[] { "Error", "ไม่พบ TempDocSpaceGate ที่ระบุ" }));
            }
            else
            {
                CopyAll(new DirectoryInfo(sourceDirectory), new DirectoryInfo(targetDirectory));
                return Ok(SetReturnStringList(new String[] { "OK", "สร้าง SpaceGate สำเร็จ" }));
            }
        }

        [HttpPost("CreateSpaceGateOwner")]
        public IActionResult CreateSpaceGateOwner(String Connection, String OfficeSpaceId, String DatabaseName, String SpaceGateIP, String SpaceGateDomain)
        {
            try
            {
                DataTable dt = SelectAllTest(Connection, OfficeSpaceId, DatabaseName, "SpaceGate", "");

                String SPACEGATE_ID = "", ID = "";

                xDoc = new XmlDocument();
                String ActionType = "";
                if (dt == null)
                {
                    SPACEGATE_ID = "1";
                    ActionType = "I";
                    String MapPath = _PathProvider.MapPath("Document/SpaceGate.xml");
                    xDoc.Load(MapPath);
                }
                else
                {
                    ID = Convert.ToString(dt.Rows[0]["ID"]);
                    ActionType = "U";
                    xDoc.LoadXml(SelectLastDocumentTest(Connection, OfficeSpaceId, DatabaseName, "SpaceGate", int.Parse(ID), ""));

                    String rootRow = "//Document/Data/Section[@Name='SpaceGateOther']/Items[@Name='SpaceGateOtherInfo']/Item";
                    XmlNodeList LogInfoNodeList = xDoc.SelectNodes(rootRow);
                    Int32 rowList = LogInfoNodeList.Count;
                    SPACEGATE_ID = Convert.ToString(rowList + 1);
                }

                NCS = new NextwaverDB.NColumns();
                NCS.Add(new NextwaverDB.NColumn("SPACEGATE_ID", SPACEGATE_ID));
                NCS.Add(new NextwaverDB.NColumn("SPACEGATE_IP", SpaceGateIP));
                NCS.Add(new NextwaverDB.NColumn("SPACEGATE_DOMAIN", SpaceGateDomain));
                NCS.Add(new NextwaverDB.NColumn("CREATE_DATE", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));

                string RootPathBook = "//Document/Data/Section[@ID='1']/Items[@Name='SpaceGate']";

                AddDataXmlNode(RootPathBook + "/Item[@Name='SPACEGATE_ID']", SPACEGATE_ID);
                AddDataXmlNode(RootPathBook + "/Item[@Name='SPACEGATE_IP']", SpaceGateIP);
                AddDataXmlNode(RootPathBook + "/Item[@Name='SPACEGATE_DOMAIN']", SpaceGateDomain);
                AddDataXmlNode(RootPathBook + "/Item[@Name='CREATE_DATE']", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));


                String XPath = "//Document/Data/Section[@ID='2']/Items[@Name='SpaceGateOtherInfo']";

                XmlNode CommentsInfoListNode = xDoc.SelectSingleNode(XPath);
                XmlNode NewCommentsfoNode;
                NewCommentsfoNode = xDoc.CreateElement("Item");

                XmlAddAttribute(xDoc, NewCommentsfoNode, "C00", SPACEGATE_ID);
                XmlAddAttribute(xDoc, NewCommentsfoNode, "C01", SpaceGateIP);
                XmlAddAttribute(xDoc, NewCommentsfoNode, "C02", SpaceGateDomain);
                CommentsInfoListNode.AppendChild(NewCommentsfoNode);

                string strDoc = xDoc.OuterXml;
                if (ActionType == "I")
                {
                    InsertDataDoc(Connection, OfficeSpaceId, DatabaseName, "SpaceGate", NCS.ExportString(), strDoc, "System");
                    InsertDataDoc(Connection, OfficeSpaceId, "File", "SpaceGate", NCS.ExportString(), strDoc, "System");
                }
                else
                {
                    NWS = new NextwaverDB.NWheres();
                    NWS.Add(new NextwaverDB.NWhere("ID", ID));

                    UpdateDataDoc(Connection, OfficeSpaceId, DatabaseName, "SpaceGate", NCS.ExportString(), NWS.ExportString(), strDoc, "system");
                    UpdateDataDoc(Connection, OfficeSpaceId, "File", "SpaceGate", NCS.ExportString(), NWS.ExportString(), strDoc, "system");
                }
                return Ok(SetReturnStringList(new String[] { "OK", "สร้าง SpaceGateOwner สำเร็จ" }));
            }
            catch (Exception ex)
            {
                return Ok(SetReturnStringList(new String[] { "ERROR", ex.Message }));
            }
        }

        [HttpPost("CreateBookClass")]
        public IActionResult CreateBookClass(String Connection, String OfficeSpaceId, String DatabaseName, String BookClassName, Int32 BookMax, Int32 PageMax, Int32 SubPageMax, Int32 RowMax)
        {
            _Connection = Connection;
            _OfficeSpaceId = OfficeSpaceId;
            _DatabaseName = DatabaseName;

            try
            {
                xDoc = new XmlDocument();
                String MapPath = _PathProvider.MapPath("Document/BookClass.xml");
                xDoc.Load(MapPath);
                NCS = new NextwaverDB.NColumns();
                NCS.Add(new NextwaverDB.NColumn("BOOK_CLASS_NAME", BookClassName));
                NCS.Add(new NextwaverDB.NColumn("BOOK_MAX", Convert.ToString(BookMax)));
                NCS.Add(new NextwaverDB.NColumn("CREATE_DATE", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));

                string RootPathBook = "//Document/Data/Section[@ID='1']/Items[@Name='BookClass']";

                AddDataXmlNode(RootPathBook + "/Item[@Name='BOOK_CLASS_NAME']", BookClassName);
                AddDataXmlNode(RootPathBook + "/Item[@Name='BOOK_MAX']", Convert.ToString(BookMax));
                AddDataXmlNode(RootPathBook + "/Item[@Name='CREATE_DATE']", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                string XPathDataRecord = "//Document/Data/Section[@ID='2']/Items[@Name='ReferenceBookInfo']";

                XmlNode CommentsInfoListNode = xDoc.SelectSingleNode(XPathDataRecord);
                XmlNode NewCommentsfoNode;
                NewCommentsfoNode = xDoc.CreateElement("Item");
                NCS.Add(new NextwaverDB.NColumn("BOOK_COUNT", "1"));
                AddDataXmlNode(RootPathBook + "/Item[@Name='BOOK_COUNT']", "1");

                string strDoc = xDoc.OuterXml;
                xDocBookClassLast = new XmlDocument();
                xDocBookClassLast.LoadXml(strDoc);

                string[] ResultBook = InsertDataDoc(_Connection, _OfficeSpaceId, _DatabaseName, "BookClass", NCS.ExportString(), strDoc, "System");
                InsertDataDoc(_Connection, _OfficeSpaceId, "File", "BookClass", NCS.ExportString(), strDoc, "System");

                String DocBookClassID = ResultBook[4];

                CreateInitialBook(DocBookClassID, BookClassName, PageMax, SubPageMax, RowMax, false);

                return Ok(SetReturnStringList(new String[] { "OK", "สร้าง Create Book Class สำเร็จ" }));
            }
            catch (Exception ex)
            {
                return Ok(SetReturnStringList(new String[] { "ERROR", ex.Message }));
            }
        }

        private void CreateInitialBook(String DocBookClassID, String BOOK_CLASS_NAME, Int32 PAGE_MAX, Int32 SUB_PAGE_MAX, Int32 ROW_MAX, Boolean newPage)
        {
            String DocBookID = Convert.ToString(bd._BookDocID);
            if (!newPage) DocBookID = CreateBook(DocBookClassID, BOOK_CLASS_NAME, Convert.ToString(PAGE_MAX));
            String DocPageID = CreatePage(DocBookID, Convert.ToString(SUB_PAGE_MAX));

            String XPath = "";
            if (!newPage)
            {
                //*Ref Book
                XPath = "//Document/Data/Section[@ID='2']/Items[@Name='ReferenceBookInfo']";
                InsertReference(DocBookClassID, "BookClass", XPath, xDocBookClassLast, int.Parse(DocBookID));
            }
            //*Ref Page
            XPath = "//Document/Data/Section[@ID='2']/Items[@Name='ReferencePageInfo']";
            InsertReference(DocBookID, "Book", XPath, xDocBookLast, int.Parse(DocPageID));

            for (int i = 0; i < SUB_PAGE_MAX; i++)
            {
                String DocSubPageID = CreateSubPage(DocPageID, Convert.ToString(ROW_MAX));
                //*Ref Page
                XPath = "//Document/Data/Section[@ID='2']/Items[@Name='ReferenceSubPageInfo']";
                InsertReference(DocPageID, "Page", XPath, xDocPageLast, int.Parse(DocSubPageID));
            }
            String RootPath = "//Document/Data/Section[@ID='1']/Items[@Name='Page']";

            NCS = new NextwaverDB.NColumns();
            AddValueColumn(ref xDocPageLast, ref NCS, RootPath, "SUB_PAGE_COUNT", SUB_PAGE_MAX.ToString());
            AddValueColumn(ref xDocPageLast, ref NCS, RootPath, "UPDATE_DATE", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
            updateDocByID(int.Parse(DocPageID), xDocPageLast, NCS, "Page");
        }
        public string CreateBook(String BOOK_CLASS_ID, String BOOK_CLASS_NAME, String PAGE_MAX)
        {
            String DocPageID = "";
            try
            {
                xDoc = new XmlDocument();

                var MapPath = _PathProvider.MapPath("Document/Book.xml");
                xDoc.Load(MapPath);
                NCS = new NextwaverDB.NColumns();
                NCS.Add(new NextwaverDB.NColumn("BOOK_CLASS_ID", BOOK_CLASS_ID));
                NCS.Add(new NextwaverDB.NColumn("BOOK_CLASS_NAME", BOOK_CLASS_NAME));
                NCS.Add(new NextwaverDB.NColumn("PAGE_COUNT", "1"));
                NCS.Add(new NextwaverDB.NColumn("PAGE_MAX", PAGE_MAX));
                NCS.Add(new NextwaverDB.NColumn("CREATE_DATE", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));

                string RootPathPage = "//Document/Data/Section[@ID='1']/Items[@Name='Book']";
                AddDataXmlNode(RootPathPage + "/Item[@Name='BOOK_CLASS_ID']", BOOK_CLASS_ID);
                AddDataXmlNode(RootPathPage + "/Item[@Name='BOOK_CLASS_NAME']", BOOK_CLASS_NAME);
                AddDataXmlNode(RootPathPage + "/Item[@Name='PAGE_COUNT']", "1");
                AddDataXmlNode(RootPathPage + "/Item[@Name='PAGE_MAX']", PAGE_MAX);
                AddDataXmlNode(RootPathPage + "/Item[@Name='CREATE_DATE']", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                string strDoc = xDoc.OuterXml;
                xDocBookLast = new XmlDocument();
                xDocBookLast.LoadXml(strDoc);

                String[] OP = InsertDataDoc(_Connection, _OfficeSpaceId, _DatabaseName, "Book", NCS.ExportString(), strDoc, "System");
                InsertDataDoc(_Connection, _OfficeSpaceId, "File", "Book", NCS.ExportString(), strDoc, "System");
                DocPageID = OP[4];

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return DocPageID;
        }
        public string CreatePage(String BOOK_ID, String SUB_PAGE_MAX)
        {
            String DocPageID = "";
            try
            {
                xDoc = new XmlDocument();

                var MapPath = _PathProvider.MapPath("Document/Page.xml");
                xDoc.Load(MapPath);

                NCS = new NextwaverDB.NColumns();
                NCS.Add(new NextwaverDB.NColumn("BOOK_ID", BOOK_ID));
                NCS.Add(new NextwaverDB.NColumn("SUB_PAGE_COUNT", "1"));
                NCS.Add(new NextwaverDB.NColumn("SUB_PAGE_MAX", SUB_PAGE_MAX));
                NCS.Add(new NextwaverDB.NColumn("CREATE_DATE", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));

                string RootPathPage = "//Document/Data/Section[@ID='1']/Items[@Name='Page']";
                AddDataXmlNode(RootPathPage + "/Item[@Name='BOOK_ID']", BOOK_ID);
                AddDataXmlNode(RootPathPage + "/Item[@Name='SUB_PAGE_COUNT']", "1");
                AddDataXmlNode(RootPathPage + "/Item[@Name='SUB_PAGE_MAX']", SUB_PAGE_MAX);
                AddDataXmlNode(RootPathPage + "/Item[@Name='CREATE_DATE']", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                string strDoc = xDoc.OuterXml;
                xDocPageLast = new XmlDocument();
                xDocPageLast.LoadXml(strDoc);

                String[] OP = InsertDataDoc(_Connection, _OfficeSpaceId, _DatabaseName, "Page", NCS.ExportString(), strDoc, "System");
                InsertDataDoc(_Connection, _OfficeSpaceId, "File", "Page", NCS.ExportString(), strDoc, "System");
                DocPageID = OP[4];

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return DocPageID;
        }
        public string CreateSubPage(String PAGE_ID, String ROW_MAX)
        {
            String DocPageID = "", SPACEGATE_ID = "1";
            try
            {
                if (PAGE_ID != "1")
                {
                    DataTable dt = SelectAllTest(_Connection, _OfficeSpaceId, _DatabaseName, "SpaceGate", "");
                    if (dt != null) SPACEGATE_ID = Convert.ToString(dt.Rows[0]["SPACEGATE_ID"]);
                }

                xDoc = new XmlDocument();

                var MapPath = _PathProvider.MapPath("Document/SubPage.xml");
                xDoc.Load(MapPath);

                NCS = new NextwaverDB.NColumns();
                NCS.Add(new NextwaverDB.NColumn("PAGE_ID", PAGE_ID));
                NCS.Add(new NextwaverDB.NColumn("ROW_COUNT", "0"));
                NCS.Add(new NextwaverDB.NColumn("ROW_MAX", ROW_MAX));
                NCS.Add(new NextwaverDB.NColumn("CREATE_DATE", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));


                if (PAGE_ID == "1") NCS.Add(new NextwaverDB.NColumn("WINNER", "1"));
                else NCS.Add(new NextwaverDB.NColumn("WINNER", SPACEGATE_ID));

                string RootPathPage = "//Document/Data/Section[@ID='1']/Items[@Name='SubPage']";
                AddDataXmlNode(RootPathPage + "/Item[@Name='PAGE_ID']", PAGE_ID);
                AddDataXmlNode(RootPathPage + "/Item[@Name='ROW_COUNT']", "0");
                AddDataXmlNode(RootPathPage + "/Item[@Name='ROW_MAX']", ROW_MAX);
                AddDataXmlNode(RootPathPage + "/Item[@Name='CREATE_DATE']", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                if (PAGE_ID == "1") AddDataXmlNode(RootPathPage + "/Item[@Name='WINNER']", "1");
                else AddDataXmlNode(RootPathPage + "/Item[@Name='WINNER']", SPACEGATE_ID);

                string strDoc = xDoc.OuterXml;
                xDocSubPageLast = new XmlDocument();
                xDocSubPageLast.LoadXml(strDoc);

                String[] OP = InsertDataDoc(_Connection, _OfficeSpaceId, _DatabaseName, "SubPage", NCS.ExportString(), strDoc, "System");
                DocPageID = OP[4];
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return DocPageID;
        }


        public void InsertReference(String DocID_Update, String Table, String XPathDataRecord, XmlDocument xDocBook, Int32 DocIDRef)
        {
            try
            {
                XmlNode CommentsInfoListNode = xDocBook.SelectSingleNode(XPathDataRecord);
                XmlNode NewCommentsfoNode;
                NewCommentsfoNode = xDocBook.CreateElement("Item");

                XmlAddAttribute(xDocBook, NewCommentsfoNode, "C00", Convert.ToString(DocIDRef));
                CommentsInfoListNode.AppendChild(NewCommentsfoNode);

                NCS = new NextwaverDB.NColumns();
                NWS = new NextwaverDB.NWheres();
                NWS.Add(new NextwaverDB.NWhere("ID", DocID_Update));

                UpdateDataDoc(_Connection, _OfficeSpaceId, _DatabaseName, Table, NCS.ExportString(), NWS.ExportString(), xDocBook.OuterXml, "system");
                UpdateDataDoc(_Connection, _OfficeSpaceId, "File", Table, NCS.ExportString(), NWS.ExportString(), xDocBook.OuterXml, "system");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void InsertDataItem(XmlDocument xDoc, Row dataRow)
        {
            //   string NWS_Encrypt =  Encrypt(dataRow);

            string XPathDataRecord = "//Document/Data/Section[@ID='2']/Items[@Name='RowInfo']";

            XmlNode CommentsInfoListNode = xDoc.SelectSingleNode(XPathDataRecord);
            XmlNode NewCommentsfoNode;
            NewCommentsfoNode = xDoc.CreateElement("Item");

            XmlAddAttribute(xDoc, NewCommentsfoNode, "C00", dataRow.DocClassID);
            XmlAddAttribute(xDoc, NewCommentsfoNode, "C01", Convert.ToString(dataRow.DocID));
            XmlAddAttribute(xDoc, NewCommentsfoNode, "C02", dataRow.Token);
            XmlAddAttribute(xDoc, NewCommentsfoNode, "C03", dataRow.Hashing);
            XmlAddAttribute(xDoc, NewCommentsfoNode, "C04", dataRow.Dsig);
            XmlAddAttribute(xDoc, NewCommentsfoNode, "C05", dataRow.SpaceGateID_Sender);
            XmlAddAttribute(xDoc, NewCommentsfoNode, "C06", dataRow.SpaceGateID_Update);
            CommentsInfoListNode.AppendChild(NewCommentsfoNode);
        }

        [HttpGet("CreateTicket")]
        public String CreateTicket(String Connection, String OfficeSpaceId, String DatabaseName, Int32 SubPageID, Int32 ItemCount)
        {
            _Connection = Connection;
            _OfficeSpaceId = OfficeSpaceId;
            _DatabaseName = DatabaseName;
            try
            {
                Int32 rowEmptry = CheckRowEmptry(SubPageID);

                Int32 itemReject = 0;
                if (rowEmptry < ItemCount)
                {
                    itemReject = ItemCount - rowEmptry;
                    return "Data is not insert over " + rowEmptry + " items";
                    //  return Ok("Data is not insert over " + rowEmptry + " items");
                }

                List<Row> dataRow = new List<Row>();
                var MapPath = _PathProvider.MapPath("Document/Ticket.xml");

                for (int nTicket = 1; nTicket <= ItemCount; nTicket++)
                {
                    xDoc = new XmlDocument();
                    xDoc.Load(MapPath);

                    string RootPathBook = "//Document/Data/Section[@ID='1']/Items[@Name='Ticket']";
                    NCS = new NextwaverDB.NColumns();
                    NCS.Add(new NextwaverDB.NColumn("CREATE_DATE", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));
                    AddDataXmlNode(RootPathBook + "/Item[@Name='CREATE_DATE']", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                    DataTable dtTicket = SelectAllTest(Connection, OfficeSpaceId, DatabaseName, "Ticket", "");

                    if (dtTicket == null || dtTicket.Rows.Count == 0)
                    {
                        AddDataXmlNode(RootPathBook + "/Item[@Name='TICKET_ID']", "1");
                        dataRow.Add(new Row() { DocClassID = "Ticket", DocID = 1, Token = "", Hashing = "", Dsig = "" });
                    }
                    else
                    {
                        DataRow lastRow = dtTicket.Rows[dtTicket.Rows.Count - 1];
                        Int32 ticketIDLast = Convert.ToInt32(lastRow["ID"]);
                        ticketIDLast++;

                        AddDataXmlNode(RootPathBook + "/Item[@Name='TICKET_ID']", Convert.ToString(ticketIDLast));
                        dataRow.Add(new Row() { DocClassID = "Ticket", DocID = ticketIDLast, Token = "", Hashing = "", Dsig = "" });
                    }

                    string strDoc = xDoc.OuterXml;
                    string[] OP = InsertDataDoc(Connection, OfficeSpaceId, DatabaseName, "Ticket", NCS.ExportString(), strDoc, "System");
                }
                String result = InsertDataRowInPage(dataRow, SubPageID);
                return result;
            }
            catch (Exception ex)
            {
                // return Ok(ex.Message);
                return ex.Message;
            }
        }

        [HttpGet("GetBookDetails")]
        public void getBookDetailsCurrent()
        {

            DataTable dtBookClass = SelectAllTest(_Connection, _OfficeSpaceId, _DatabaseName, "BookClass", "system");
            DataRow lastBookClass = dtBookClass.Rows[dtBookClass.Rows.Count - 1];
            bd._BookClassDocID = Int32.Parse(Convert.ToString(lastBookClass["ID"]));
            bd._BookClassName = Convert.ToString(lastBookClass["BOOK_CLASS_NAME"]);
            bd._BookCount = Int32.Parse(Convert.ToString(lastBookClass["BOOK_COUNT"]));
            bd._BookMax = Int32.Parse(Convert.ToString(lastBookClass["BOOK_MAX"]));
            string strDoc = SelectLastDocumentTest(_Connection, _OfficeSpaceId, _DatabaseName, "BookClass", bd._BookClassDocID, "system");
            xDocBookClassLast = new XmlDocument();
            xDocBookClassLast.LoadXml(strDoc);

            DataTable dtBook = SelectAllTest(_Connection, _OfficeSpaceId, _DatabaseName, "Book", "system");
            DataRow lastBook = dtBook.Rows[dtBook.Rows.Count - 1];
            bd._BookDocID = Int32.Parse(Convert.ToString(lastBook["ID"]));
            bd._PageCount = Int32.Parse(Convert.ToString(lastBook["PAGE_COUNT"]));
            bd._PageMax = Int32.Parse(Convert.ToString(lastBook["PAGE_MAX"]));
            strDoc = SelectLastDocumentTest(_Connection, _OfficeSpaceId, _DatabaseName, "Book", bd._BookClassDocID, "system");
            xDocBookLast = new XmlDocument();
            xDocBookLast.LoadXml(strDoc);

            DataTable dtPage = SelectAllTest(_Connection, _OfficeSpaceId, _DatabaseName, "Page", "system");
            DataRow lastPage = dtPage.Rows[dtPage.Rows.Count - 1];
            bd._SubPageMax = Int32.Parse(Convert.ToString(lastPage["SUB_PAGE_MAX"]));

            DataTable dtSubPage = SelectAllTest(_Connection, _OfficeSpaceId, _DatabaseName, "SubPage", "system");
            DataRow lastSubPage = dtSubPage.Rows[dtSubPage.Rows.Count - 1];
            bd._RowMax = Int32.Parse(Convert.ToString(lastSubPage["ROW_MAX"]));
        }

        private Int32 CheckRowEmptry(Int32 SubPageID)
        {
            getLastSubPage(SubPageID);

            if (spl.ROW_MAX == spl.ROW_COUNT)
                return 0;
            else
                return spl.ROW_MAX - spl.ROW_COUNT;
        }
        public void getLastSubPage(Int32 ID)
        {
            try
            {
                NWS = new NextwaverDB.NWheres();
                NWS.Add(new NextwaverDB.NWhere("ID", Convert.ToString(ID)));
                DataTable dt = SelectAllColumnByWhereTest(_Connection, _OfficeSpaceId, _DatabaseName, "SubPage", NWS.ExportString(), "system");
                if (dt.Rows.Count > 0)
                {
                    spl.DocSubPageID = Convert.ToInt32(dt.Rows[0]["ID"]);
                    spl.PAGE_ID = Convert.ToInt32(dt.Rows[0]["PAGE_ID"]);
                    spl.ROW_COUNT = Convert.ToInt32(dt.Rows[0]["ROW_COUNT"]);
                    spl.ROW_MAX = Convert.ToInt32(dt.Rows[0]["ROW_MAX"]);
                }

                string strDoc = SelectLastDocumentTest(_Connection, _OfficeSpaceId, _DatabaseName, "SubPage", spl.DocSubPageID, "system");
                xDocSubPageLast = new XmlDocument();
                xDocSubPageLast.LoadXml(strDoc);
            }
            catch (Exception ex)
            {

            }
        }
        [HttpGet("InsertDataRowInPage")]
        public String InsertDataRowInPage(List<Row> dataRow, Int32 SubPageID)
        {
            try
            {
                //=========InsertDataRow==================
                for (int row = 0; row < dataRow.Count; row++)
                {
                    InsertDataItem(xDocSubPageLast, dataRow[row]);
                    spl.ROW_COUNT++;
                }

                String RootPath = "//Document/Data/Section[@ID='1']/Items[@Name='SubPage']";

                NCS = new NextwaverDB.NColumns();
                AddValueColumn(ref xDocSubPageLast, ref NCS, RootPath, "ROW_COUNT", Convert.ToString(spl.ROW_COUNT));
                AddValueColumn(ref xDocSubPageLast, ref NCS, RootPath, "UPDATE_DATE", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                updateDocByID(spl.DocSubPageID, xDocSubPageLast, NCS, "SubPage");

                //========CHECK ALL SUB_PAGE IS FULL============
                //**ALL SUB_PAGE FULL  >> NEW PAGE, BOOK 
                NWS = new NextwaverDB.NWheres();
                NWS.Add(new NextwaverDB.NWhere("PAGE_ID", Convert.ToString(spl.PAGE_ID)));
                DataTable dt = SelectAllColumnByWhereTest(_Connection, _OfficeSpaceId, _DatabaseName, "SubPage", NWS.ExportString(), "system");

                Int32 CountIsFull = 0;
                for (int sp = 0; sp < dt.Rows.Count; sp++)
                {
                    Int32 ROW_COUNT = Convert.ToInt32(dt.Rows[sp]["ROW_COUNT"]);
                    Int32 ROW_MAX = Convert.ToInt32(dt.Rows[sp]["ROW_MAX"]);
                    if (ROW_COUNT == ROW_MAX) CountIsFull++; ;
                }
                if (CountIsFull == dt.Rows.Count)
                {
                    getBookDetailsCurrent();
                    if (bd._PageCount < bd._PageMax)
                    {
                        // Create New Page
                        CreateInitialBook(Convert.ToString(bd._BookDocID), bd._BookClassName, bd._PageMax, bd._SubPageMax, bd._RowMax, true);
                        RootPath = "//Document/Data/Section[@ID='1']/Items[@Name='Book']";

                        NCS = new NextwaverDB.NColumns();
                        AddValueColumn(ref xDocBookLast, ref NCS, RootPath, "PAGE_COUNT", (bd._PageCount + 1).ToString());
                        AddValueColumn(ref xDocBookLast, ref NCS, RootPath, "UPDATE_DATE", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                        updateDocByID(bd._BookDocID, xDocBookLast, NCS, "Book");

                    }
                    else if (bd._BookCount < bd._BookMax)
                    {
                        // Create New Book
                        CreateInitialBook(Convert.ToString(bd._BookClassDocID), bd._BookClassName, bd._PageMax, bd._SubPageMax, bd._RowMax, false);
                        RootPath = "//Document/Data/Section[@ID='1']/Items[@Name='BookClass']";

                        NCS = new NextwaverDB.NColumns();
                        AddValueColumn(ref xDocBookLast, ref NCS, RootPath, "BOOK_COUNT", (bd._BookCount + 1).ToString());
                        AddValueColumn(ref xDocBookLast, ref NCS, RootPath, "UPDATE_DATE", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                        updateDocByID(bd._BookDocID, xDocBookLast, NCS, "BookClass");
                    }
                }
                return "OK";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        #endregion



        #region  SPACEGATE SERVIVE LAST VERSION EDITOR 03-07-2019 TEST
 
        #region QueueProcessing
        [HttpGet("CheckQueueIsProcessing")]
        public Boolean CheckQueueIsProcessing()
        {
            String PathTask = _PathProvider.MapPath("Task/");
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(System.IO.File.ReadAllText(_PathProvider.MapPath(PathTask + "/TaskList.xml")));

            XmlNodeList QueueList = xDoc.SelectNodes("//QueueProcessing/Queue");

            //  <QueueProcessing>
            //  	<Queue QFileName="15622221237509032.xml" QStatus="Processing" />
            //  	<Queue QFileName="15622221251024456.xml" QStatus="Wait" />
            //  </QueueProcessing>
            foreach (XmlNode item in QueueList)
            {
                if (item.Attributes.Item(1).Value == "Processing") return true;
            }

            return false;
        }
        [HttpGet("AddQueue")]
        public void AddQueue(String Connection, String OfficeSpaceID, String DatabaseName, String EncryptData)
        {
            String Data = @"<ProcessInformation>
                            	<Connection>" + Connection + @"</Connection>
                            	<OfficeSpaceId>" + OfficeSpaceID + @"</OfficeSpaceId>
                            	<DatabaseName>" + DatabaseName + @"</DatabaseName>
                            	<EncryptData>" + EncryptData + @"</EncryptData>
                            </ProcessInformation>";

            String PathTask = _PathProvider.MapPath("Task");
            String TimeStamp = GetTimestamp();

            if (!System.IO.Directory.Exists(PathTask + "/Queue")) System.IO.Directory.CreateDirectory(PathTask + "/Queue");

            using (System.IO.StreamWriter _file = new System.IO.StreamWriter(PathTask + "/Queue/" + TimeStamp + ".xml", true))
            {
                _file.WriteLine(Data);
            }

            if (!System.IO.File.Exists(PathTask + "/TaskList.xml"))
            {
                String TaskListTmp = "<QueueProcessing></QueueProcessing>";
                using (System.IO.StreamWriter _file = new System.IO.StreamWriter(PathTask + "/TaskList.xml", true))
                {
                    _file.WriteLine(TaskListTmp);
                }
            }

            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(System.IO.File.ReadAllText(_PathProvider.MapPath(PathTask + "/TaskList.xml")));

            //  <Queue QFile="Q03-04-2019-2-01-54" QStatus="Processing"/>
            XmlElement xmlElement = xDoc.CreateElement("Queue");
            xmlElement.Attributes.Append(XmlAttribute("QFileName", TimeStamp + ".xml", ref xDoc));
            xmlElement.Attributes.Append(XmlAttribute("QStatus", "Wait", ref xDoc));

            XmlNode node = xDoc.SelectSingleNode("//QueueProcessing");
            node.AppendChild(xmlElement);

            System.IO.File.WriteAllText(_PathProvider.MapPath(PathTask + "/TaskList.xml"), String.Empty);
            using (System.IO.StreamWriter _file = new System.IO.StreamWriter(PathTask + "/TaskList.xml", true))
            {
                _file.WriteLine(xDoc.OuterXml);
            }

        }
        private String QueueProcessing()
        {
            String PathTask = _PathProvider.MapPath("Task/");
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(System.IO.File.ReadAllText(_PathProvider.MapPath(PathTask + "/TaskList.xml")));

            XmlNodeList QueueList = xDoc.SelectNodes("//QueueProcessing/Queue[@QStatus='Wait']");

            //  <QueueProcessing>
            //  	<Queue QFileName="15622221237509032.xml" QStatus="Processing" />
            //  	<Queue QFileName="15622221251024456.xml" QStatus="Wait" />
            //  </QueueProcessing>
            String xDocQueueProcess = "";
            foreach (XmlNode item in QueueList)
            {
                String FileName = item.Attributes.Item(0).Value;
                // Change Status Test
                String xEdit = xDoc.OuterXml.Replace(item.OuterXml, "<Queue QFileName=\"" + FileName + "\" QStatus=\"Processing\" />");

                System.IO.File.WriteAllText(_PathProvider.MapPath(PathTask + "/TaskList.xml"), String.Empty);
                using (System.IO.StreamWriter _file = new System.IO.StreamWriter(PathTask + "/TaskList.xml", true))
                {
                    _file.WriteLine(xEdit);
                }

                xDocQueueProcess = System.IO.File.ReadAllText(_PathProvider.MapPath(PathTask + "/Queue/" + FileName));
                break;
            }
            return xDocQueueProcess;
        }
        private Int32 QueueWaiting()
        {
            String PathTask = _PathProvider.MapPath("Task/");
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(System.IO.File.ReadAllText(_PathProvider.MapPath(PathTask + "/TaskList.xml")));

            XmlNodeList QueueList = xDoc.SelectNodes("//QueueProcessing/Queue[@QStatus='Wait']");
            return QueueList.Count;

        }
        private void QueueSuccess()
        {
            String PathTask = _PathProvider.MapPath("Task/");
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(System.IO.File.ReadAllText(_PathProvider.MapPath(PathTask + "/TaskList.xml")));

            XmlNodeList QueueList = xDoc.SelectNodes("//QueueProcessing/Queue[@QStatus='Processing']");

            foreach (XmlNode item in QueueList)
            {
                String FileName = item.Attributes.Item(0).Value;
                // Change Status Test
                String xEdit = xDoc.OuterXml.Replace(item.OuterXml, "");

                System.IO.File.WriteAllText(_PathProvider.MapPath(PathTask + "/TaskList.xml"), String.Empty);
                using (System.IO.StreamWriter _file = new System.IO.StreamWriter(PathTask + "/TaskList.xml", true))
                {
                    _file.WriteLine(xEdit);
                }
                System.IO.File.Delete(_PathProvider.MapPath("Task/Queue/" + FileName));
            }
        }

        #endregion

        [HttpPost("flowTest")]
        public void flowTest(String Connection, String OfficeSpaceId, String DatabaseName)
        {
            try
            {
                String DataTicket = "";
                var file = Request.Form.Files[0];
                if (file.Length > 0)
                {
                    var Stream = file.OpenReadStream();
                    using (var streamReader = new StreamReader(Stream, Encoding.UTF8))
                    {
                        DataTicket = streamReader.ReadToEnd();
                    }
                }

                //  1.get Last PageID
                DataTable dtPage = SelectAllTest(Connection, OfficeSpaceId, DatabaseName, "Page", "system");
                DataRow lastPage = dtPage.Rows[dtPage.Rows.Count - 1];
                String _PageID = Convert.ToString(lastPage["ID"]);

                //  2. check SubPageOwner in Folder DiffFile
                String PathFiles = _PathProvider.MapPath("Store/OF.0001/database/File/SubPage/DB-Doc$TB-SubPage/LV");
                String[] _FolderSubPage = System.IO.Directory.GetDirectories(PathFiles, "*", System.IO.SearchOption.TopDirectoryOnly);
                if (_FolderSubPage.Length != 0)
                {
                    string[] _FileList = System.IO.Directory.GetFiles(_FolderSubPage[_FolderSubPage.Length - 1], "*.xml", SearchOption.AllDirectories);
                    XmlDocument xDocIndexSubPage = new XmlDocument();
                    xDocIndexSubPage.LoadXml(System.IO.File.ReadAllText(_FileList[_FileList.Length - 1]));

                    // Filter by PageID
                    String rootItemDataPage = "//Document/Data/Section[@ID='Doc']/Items[@ID='SubPage']/Item";
                    String FindFiltter = rootItemDataPage + "[@" + "C01" + @" = '" + _PageID + @"']";
                    XmlNodeList InfoNodeList = xDocIndexSubPage.SelectNodes(FindFiltter);

                    //  3. Random SubPage
                    ArrayList SubPageNoList = new ArrayList();
                    foreach (XmlNode item in InfoNodeList)
                    {
                        Int32 RowCount = Int32.Parse(item.Attributes["C03"].Value);
                        Int32 RowMax = Int32.Parse(item.Attributes["C04"].Value);
                        if (RowCount < RowMax)
                        {
                            SubPageNoList.Add(item.Attributes["ID"].Value + "|" + item.Attributes["C02"].Value);
                        }
                    }
                    var random = new Random();
                    int index = random.Next(SubPageNoList.Count);

                    String _SubPageID = SubPageNoList[index].ToString();
                    //  4. Get OwnerSubPage IP
                    String SubPageID = _SubPageID.Split('|')[0];
                    String SubPageOwner = _SubPageID.Split('|')[1];

                    String[] SpaceGateOwner_Sender = CheckOwnerSubPage(Connection, OfficeSpaceId, DatabaseName, SubPageOwner);
                    String SpaceGateSenderID = SpaceGateOwner_Sender[0];
                    String SpaceGateOwnerIP = SpaceGateOwner_Sender[1];

                    //  5. sent TicketList to SpaceGateOwner
                    //   InsertDataRowInSubPageTest();

                    String url = SpaceGateOwnerIP + "/InsertDataRowInSubPageTest?Connection=NextwaverDatabase&OfficeSpaceId=OF.0001&DatabaseName=Doc&SubPageID=" + SubPageID + "&Sender=" + SpaceGateSenderID + "&SpaceGateOwnerIP=" + SpaceGateOwnerIP;
                    String Result = CallAPI(url, Method.Post, DataTicket);
                }
            }
            catch (Exception ex)
            {
                CreateLog("flowTest >>>> " + ex.Message);
            }
        }

        [HttpPost("InsertDataRowInSubPageTest")]
        public void InsertDataRowInSubPageTest(String Connection, String OfficeSpaceId, String DatabaseName, Int32 SubPageID, String Sender, String SpaceGateOwnerIP)
        {
            try
            {
                _Connection = Connection;
                _OfficeSpaceId = OfficeSpaceId;
                _DatabaseName = DatabaseName;

                string EncryptData = "";
                var file = Request.Form.Files[0];
                if (file.Length > 0)
                {
                    var Stream = file.OpenReadStream();
                    using (var streamReader = new StreamReader(Stream, Encoding.UTF8))
                    {
                        EncryptData = streamReader.ReadToEnd();
                    }
                }

                //  Queue Processing
                AddQueue(Connection, OfficeSpaceId, DatabaseName, EncryptData);

                List<TicketList> _DataUpdate = new List<TicketList>();
                // while (QueueWaiting() > 0)
                {
                    if (!CheckQueueIsProcessing())
                    {
                        // Start QueueProcessing
                        XmlDocument xDocQueueProcess = new XmlDocument();
                        xDocQueueProcess.LoadXml(QueueProcessing());

                        _Connection = xDocQueueProcess.ChildNodes[0].ChildNodes[0].InnerXml;
                        _OfficeSpaceId = xDocQueueProcess.ChildNodes[0].ChildNodes[1].InnerXml;
                        _DatabaseName = xDocQueueProcess.ChildNodes[0].ChildNodes[2].InnerXml;
                        String DecryptData = Decrypt(xDocQueueProcess.ChildNodes[0].ChildNodes[3].InnerXml);

                        var objects = JsonConvert.DeserializeObject<List<object>>(DecryptData);

                        string[] Jsonn = objects.Select(x => x.ToString()).ToArray();

                        List<Row> dataRow = new List<Row>();
                        for (int i = 0; i < Jsonn.Length; i++)
                        {
                            var _Jobject = JObject.Parse(Jsonn[i]);
                            String _DocClassID = _Jobject["DocClassID"].ToString();
                            Int32 _DocID = Int32.Parse(_Jobject["DocID"].ToString());
                            String _Token = _Jobject["Token"].ToString();
                            String _Hashing = _Jobject["Hashing"].ToString();
                            String _Dsig = _Jobject["Dsig"].ToString();
                            String _SpaceGateID_Sender = Sender;
                            String _SpaceGateID_Update = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                            dataRow.Add(new Row() { DocClassID = _DocClassID, DocID = _DocID, Token = _Token, Hashing = _Hashing, Dsig = _Dsig, SpaceGateID_Sender = _SpaceGateID_Sender, SpaceGateID_Update = _SpaceGateID_Update });

                        }

                        //##SubPageOwner Update
                        //  6. count row max in SubPage with TicketList 
                        //      if(Rows.isFull)
                        //          Redo 1

                        String RootPath = "//Document/Data/Section[@ID='1']/Items[@Name='SubPage']";
                        CreateTempDiff();
                        for (int row = 0; row < dataRow.Count; row++)
                        {
                            getLastSubPage(SubPageID);
                            if (spl.ROW_COUNT < spl.ROW_MAX)
                            {
                                InsertDataItem(xDocSubPageLast, dataRow[row]);
                                spl.ROW_COUNT++;

                                NCS = new NextwaverDB.NColumns();
                                AddValueColumn(ref xDocSubPageLast, ref NCS, RootPath, "ROW_COUNT", spl.ROW_COUNT.ToString());
                                AddValueColumn(ref xDocSubPageLast, ref NCS, RootPath, "UPDATE_DATE", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                                updateDocByID(spl.DocSubPageID, xDocSubPageLast, NCS, "SubPage");
                            }
                            else
                            {
                                _DataUpdate.Add(new TicketList() { DocClassID = dataRow[row].DocClassID, DocID = dataRow[row].DocID, Token = dataRow[row].Token, Hashing = dataRow[row].Hashing, Dsig = dataRow[row].Dsig });
                            }
                        }

                        // CHECK FULL SUBPAGE
                        CheckSubPageIsFull(RootPath, Convert.ToString(spl.PAGE_ID));

                        CreateLog("IS FULL **** 2");
                        // PUSH DATA
                        PushFileDiff(SpaceGateOwnerIP);

                        QueueSuccess();
                    }
                }
                if (_DataUpdate.Count > 0)
                {
                    string dataRow_Encrypt = Encrypt(JsonConvert.SerializeObject(_DataUpdate));
                    //  String url = "https://localhost:44387/WS/V1/flowTest?Connection=NextwaverDatabase&OfficeSpaceId=OF.0001&DatabaseName=Doc";
                    String url = SpaceGateOwnerIP + "/flowTest?Connection=NextwaverDatabase&OfficeSpaceId=OF.0001&DatabaseName=Doc";
                    CallAPI(url, Method.Post, dataRow_Encrypt);
                }
            }
            catch (Exception ex)
            {
                CreateLog("InsertDataRowInSubPageTest >>>> " + ex.Message);
            }
        }

        private void CheckSubPageIsFull(String RootPath, String PageID)
        {
            NWS = new NextwaverDB.NWheres();
            NWS.Add(new NextwaverDB.NWhere("PAGE_ID", PageID));
            DataTable dt = SelectAllColumnByWhereTest(_Connection, _OfficeSpaceId, _DatabaseName, "SubPage", NWS.ExportString(), "system");

            Int32 CountIsFull = 0;

            for (int sp = 0; sp < dt.Rows.Count; sp++)
            {
                Int32 ROW_COUNT = Convert.ToInt32(dt.Rows[sp]["ROW_COUNT"]);
                Int32 ROW_MAX = Convert.ToInt32(dt.Rows[sp]["ROW_MAX"]);
                if (ROW_COUNT == ROW_MAX) CountIsFull++; ;
            }
            if (CountIsFull == dt.Rows.Count)
            {
                getBookDetailsCurrent();
                if (bd._PageCount < bd._PageMax)
                {
                    // Create New Page
                    CreateInitialBook(Convert.ToString(bd._BookDocID), bd._BookClassName, bd._PageMax, bd._SubPageMax, bd._RowMax, true);
                    RootPath = "//Document/Data/Section[@ID='1']/Items[@Name='Book']";

                    NCS = new NextwaverDB.NColumns();
                    AddValueColumn(ref xDocBookLast, ref NCS, RootPath, "PAGE_COUNT", (bd._PageCount + 1).ToString());
                    AddValueColumn(ref xDocBookLast, ref NCS, RootPath, "UPDATE_DATE", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                    updateDocByID(bd._BookDocID, xDocBookLast, NCS, "Book");
                }
                else if (bd._BookCount < bd._BookMax)
                {
                    // Create New Book
                    CreateInitialBook(Convert.ToString(bd._BookClassDocID), bd._BookClassName, bd._PageMax, bd._SubPageMax, bd._RowMax, false);
                    RootPath = "//Document/Data/Section[@ID='1']/Items[@Name='BookClass']";

                    NCS = new NextwaverDB.NColumns();
                    AddValueColumn(ref xDocBookClassLast, ref NCS, RootPath, "BOOK_COUNT", (bd._BookCount + 1).ToString());
                    AddValueColumn(ref xDocBookClassLast, ref NCS, RootPath, "UPDATE_DATE", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                    updateDocByID(bd._BookClassDocID, xDocBookClassLast, NCS, "BookClass");
                }
                CreateLog("IS FULL **** 1");
                CreateLog("START PAGE IS FULL");
                CreateLog("===================== OLD ========================");
                CreateLog(System.IO.File.ReadAllText(_PathProvider.MapPath("Temp.txt")));

                CreateLog("===================== LAST ========================");
                String PathFiles = _PathProvider.MapPath("Store/OF.0001/database/File/SubPage");
                XmlDocument _DiffPathLocalNew = new XmlDocument();
                GetFileSystemInfoList(ref _DiffPathLocalNew, PathFiles);

                CreateLog(_DiffPathLocalNew.OuterXml);
                CreateLog("END");
            }
        }
 

        #endregion

        #region SPACEGATE SERVIVE LAST VERSION EDITOR 31-05-2019

        //GetByServerLocal
        public String GetLastSubPageCurrent(String _Connection, String _OfficeSpaceId, String _DatabaseName)
        {
            DataTable dtPage = SelectAllTest(_Connection, _OfficeSpaceId, _DatabaseName, "Page", "system");
            DataRow lastPage = dtPage.Rows[dtPage.Rows.Count - 1];
            // bd._SubPageMax = Int32.Parse(Convert.ToString(lastPage["SUB_PAGE_MAX"]));
            String _PageID = Convert.ToString(lastPage["ID"]);


            NWS = new NextwaverDB.NWheres();
            NWS.Add(new NextwaverDB.NWhere("PAGE_ID", _PageID));
            DataTable dt = SelectAllColumnByWhereTest(_Connection, _OfficeSpaceId, _DatabaseName, "SubPage", NWS.ExportString(), "system");

            ArrayList SubPageNoList = new ArrayList();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                Int32 _RowCount = Convert.ToInt32(dt.Rows[i]["ROW_COUNT"]);
                Int32 _RowMax = Convert.ToInt32(dt.Rows[i]["ROW_MAX"]);
                if (_RowCount < _RowMax)
                {
                    SubPageNoList.Add(dt.Rows[i]["ID"].ToString());
                    // _SubPageID = Convert.ToString(dt.Rows[i]["ID"]);
                    // break;
                }
            }

            var random = new Random();
            int index = random.Next(SubPageNoList.Count);

            return SubPageNoList[index].ToString();
        }

        public String[] CheckOwnerSubPage(String _Connection, String _OfficeSpaceId, String _DatabaseName, String OwnerSubPageID)
        {
            String[] SpaceGateIP_owner = new String[2];

            string strDoc = SelectLastDocumentTest(_Connection, _OfficeSpaceId, _DatabaseName, "SpaceGate", 1, "system");
            XmlDocument xDocSpaceGate = new XmlDocument();
            xDocSpaceGate.LoadXml(strDoc);

            String rootRow = "//Document/Data/Section[@Name='SpaceGateOther']/Items[@Name='SpaceGateOtherInfo']/Item[@C00='" + OwnerSubPageID + "']";
            XmlNodeList InfoNodeList = xDocSpaceGate.SelectNodes(rootRow);

            foreach (XmlNode item in InfoNodeList)
            {
                SpaceGateIP_owner[1] = item.Attributes.Item(1).Value;  // SpaceGateIP
            }

            //GetSpaceGateID_Sender
            DataTable dtSpaceGate = SelectAllTest(_Connection, _OfficeSpaceId, _DatabaseName, "SpaceGate", "system");
            SpaceGateIP_owner[0] = dtSpaceGate.Rows[0][1].ToString();
            return SpaceGateIP_owner;
        }
        #endregion

        #region  SynchonizeFileSystem
        [HttpGet("CreateTempDiff")]
        public void CreateTempDiff()
        {
            try
            {
                String PathFiles = _PathProvider.MapPath("Store/OF.0001/database/File/SubPage");
                XmlDocument _dirFileDiff = new XmlDocument();
                GetFileSystemInfoList(ref _dirFileDiff, PathFiles);

                if (!System.IO.File.Exists(_PathProvider.MapPath("Temp.txt")))
                {
                    System.IO.File.WriteAllText(_PathProvider.MapPath("Temp.txt"), String.Empty);
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(_PathProvider.MapPath("Temp.txt"), true))
                    {
                        file.WriteLine(_dirFileDiff.OuterXml);
                    }
                }
                else
                {
                    System.IO.File.WriteAllText(_PathProvider.MapPath("Temp.txt"), String.Empty);
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(_PathProvider.MapPath("Temp.txt"), true))
                    {
                        file.WriteLine(_dirFileDiff.OuterXml);
                    }
                }
            }
            catch (Exception ex)
            {
                CreateLog("CreateTempDiff >>>> " + ex.Message);
            }
        }

        [HttpGet("PushFileDiff")]
        public void PushFileDiff(String SpaceGateOwnerIP)
        {
            try
            {
                XmlDocument xDocDiffForPushData = new XmlDocument();
                CreateDiffForPushData(ref xDocDiffForPushData);

                // GET SPAGE GATE OTHER LIST
                string strDoc = SelectLastDocumentTest(_Connection, _OfficeSpaceId, _DatabaseName, "SpaceGate", 1, "system");
                XmlDocument xDocSpaceGate = new XmlDocument();
                xDocSpaceGate.LoadXml(strDoc);

                String rootRow = "//Document/Data/Section[@Name='SpaceGateOther']/Items[@Name='SpaceGateOtherInfo']/Item";
                XmlNodeList InfoNodeList = xDocSpaceGate.SelectNodes(rootRow);

                foreach (XmlNode item in InfoNodeList)
                {
                    // SpaceGateIP
                    String SpaceGateIP = item.Attributes.Item(1).Value;
                    if (SpaceGateIP != SpaceGateOwnerIP)
                    {
                        // PUSH DATA
                        String Result = CallAPI(SpaceGateIP + "/SynchonizeFileSystem", Method.Post, xDocDiffForPushData.OuterXml);
                    }
                }
                if (System.IO.File.Exists(_PathProvider.MapPath("Temp.txt")))
                {
                    System.IO.File.WriteAllText(_PathProvider.MapPath("Temp.txt"), String.Empty);
                }
            }
            catch (Exception ex)
            {
                CreateLog("PushFileDiff >>>> " + ex.Message);
            }
        }
 

        [HttpGet("CreateDiff")]
        public void CreateDiffForPushData(ref XmlDocument xDocDiffForPushData)
        {
            try
            {
                XmlDocument _DiffPathLocalLast = new XmlDocument();
                //  GetFileSystemInfoList(ref _DiffPathLocalLast, _PathProvider.MapPath("Temp.txt"));
                _DiffPathLocalLast.LoadXml(System.IO.File.ReadAllText(_PathProvider.MapPath("Temp.txt")));

                String PathFiles = _PathProvider.MapPath("Store/OF.0001/database/File/SubPage");
                XmlDocument _DiffPathLocalNew = new XmlDocument();
                GetFileSystemInfoList(ref _DiffPathLocalNew, PathFiles);
                //     _DiffPathLocalNew.LoadXml(System.IO.File.ReadAllText(@"C:\Users\PC-HPcompaq-Pro6300\Music\Test\New.txt"));

                var node1 = XElement.Parse(_DiffPathLocalLast.OuterXml).CreateReader();
                var node2 = XElement.Parse(_DiffPathLocalNew.OuterXml).CreateReader();

                var result = new XDocument();
                var writer = result.CreateWriter();

                var diff = new Microsoft.XmlDiffPatch.XmlDiff();
                diff.Compare(node1, node2, writer);
                writer.Flush();
                writer.Close();

                XmlDocument xDocDiff = new XmlDocument();
                xDocDiff.LoadXml(result.ToString());


                XmlElement xmlElement = xDocDiffForPushData.CreateElement("FileList");
                xDocDiffForPushData.AppendChild(xmlElement);
                // Checke Diff Lists
                if (xDocDiff.GetElementsByTagName("xd:xmldiff")[0].InnerXml != "")
                {
                    XmlNodeList elemFile = xDocDiff.GetElementsByTagName("file");


                    // Store\OF.0001\database\File\SubPage\DB-Doc$TB-SubPage\CV\0001\**0000001\0001\0000011.diff
                    // Add File LAST VERSION
                    foreach (XmlNode item in elemFile)
                    {
                        String FileName = item.Attributes.Item(6).Value;
                        String FilePath = item.Attributes.Item(2).Value;
                        String FileExtension = item.Attributes.Item(1).Value;

                        if (FileExtension == ".xml")
                        {
                            AddItemFile(ref xDocDiffForPushData, "//FileList", FileName, FilePath, FilePath, FileExtension);
                            AddXmlCDataSection(ref xDocDiffForPushData, FilePath);
                        }
                    }

                    // Add File Controll Version
                    foreach (XmlNode item in elemFile)
                    {
                        String FileName = item.Attributes.Item(6).Value;
                        String FilePath = item.Attributes.Item(2).Value;
                        String FileExtension = item.Attributes.Item(1).Value;

                        String[] spFilePath = FilePath.Split('\\');
                        String FilePathLV = "";

                        for (int i = 0; i <= 8; i++)
                        {
                            if (FilePathLV == "")
                                FilePathLV += spFilePath[i];
                            else if (i == 6)
                                FilePathLV += @"\LV";
                            else
                                FilePathLV += @"\" + spFilePath[i];

                        }
                        FilePathLV += ".xml";
                        FilePathLV = FilePathLV.Replace(".xml.xml", ".xml");

                        if (FileExtension == ".diff")
                        {
                            AddItemFile(ref xDocDiffForPushData, "//FileList", FileName, FilePath, FilePathLV, FileExtension);
                            AddXmlCDataSection(ref xDocDiffForPushData, FilePath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CreateLog("CreateDiffForPushData >>>> " + ex.Message);
            }
        }
      

        [HttpPost("SynchonizeFileSystem")]
        public String SynchonizeFileSystem()
        {
            try
            {
                XmlDocument xDoc = new XmlDocument();

                var file = Request.Form.Files[0];
                if (file.Length > 0)
                {
                    String DiffFile = "";
                    var Stream = file.OpenReadStream();
                    using (var streamReader = new StreamReader(Stream, Encoding.UTF8))
                    {
                        DiffFile = streamReader.ReadToEnd();
                    }

                    xDoc.LoadXml(DiffFile);
                    XmlNodeList elemFile = xDoc.GetElementsByTagName("File");

                    foreach (XmlNode item in elemFile)
                    {
                        String FilePath = item.Attributes.Item(1).Value;
                        String FileName = item.Attributes.Item(0).Value;
                        String FilePathFull = _PathProvider.MapPath(item.Attributes.Item(1).Value);
                        String FilePathLV = _PathProvider.MapPath(item.Attributes.Item(2).Value);
                        String FileExtension = item.Attributes.Item(3).Value;
                        String PathFolder = FilePathFull.Replace(@"\" + FileName, "");

                        // Check Folder 
                        if (!System.IO.Directory.Exists(PathFolder)) System.IO.Directory.CreateDirectory(PathFolder);

                        // Add File LAST VERSION
                        if (FileExtension == ".xml") WriteFile_LV(FilePath, item);


                        // Add File LAST CONTROL VERSION
                        if (FileExtension == ".diff") WriteFile_CV(FilePath, FilePathFull, FilePathLV, FileName, item);

                    }
                }
                return "OK";
            }
            catch (Exception ex)
            {
                CreateLog("SynchonizeFileSystem >>>> " + ex.Message);
                return ex.Message;
            }
        }

  
        #endregion

        #region Save File To Doc/Fact/File  **TEST**
        public string[] InsertDataDoc(String Connection, String OfficeSpaceId, String DatabaseName, String TableName, String NColumns_String, String strDOC, String User)
        {
            _UserName = User;

            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                SetLog("InsertData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, "", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง");

                //  return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง", "", "", "", "" }));
                string[] output = { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" };
                return output;

            }

            NextwaverDB.NColumns NCS = new NextwaverDB.NColumns();

            NCS.strXML = strDOC;

            if (!NCS.ImportString(NColumns_String))
            {
                SetLog("InsertData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, "", NCS.ErrorMSG);

                string[] strList = { "Error", NCS.ErrorMSG };
                return strList;
                //  return Ok(SetReturnStringList(new String[] { "Error", NCS.ErrorMSG, "", "", "", "" }));
            }

            NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, _PathProvider.MapPath(""));

            if (NDB.insert(DatabaseName, TableName, NCS))
            {
                String Version = "";

                String VersionDoc = "";

                XmlDocument xDoc = new XmlDocument();

                xDoc.Load(NDB.OutputXmlFile);

                String ObjectId = "DB-" + DatabaseName + "$TB-" + TableName;

                try
                {
                    ReturnStringList returnStringList = _SaveDocumentTest(OfficeSpaceId, ObjectId, NDB.OutputFileID, xDoc.OuterXml, DatabaseName, TableName);

                    if (returnStringList.DataList[0].Data == "")
                    {
                        Version = returnStringList.DataList[2].Data;
                    }
                    else
                    {
                        throw new Exception(returnStringList.DataList[1].Data);
                    }
                }
                catch (Exception ex)
                {
                    SetLog("SaveDocument", OfficeSpaceId, ObjectId, NDB.OutputFileID, xDoc.OuterXml, "", "", ex.Message);
                }

                if (NCS.strXML != "")
                {
                    xDoc = new XmlDocument();

                    xDoc.LoadXml(NCS.strXML);

                    ObjectId = "DB-" + DatabaseName + "$TB-" + TableName + "$DOC";

                    try
                    {
                        ReturnStringList returnStringList = _SaveDocumentTest(OfficeSpaceId, ObjectId, NDB.OutputFileID, xDoc.OuterXml, DatabaseName, TableName);

                        if (returnStringList.DataList[0].Data == "")
                        {
                            VersionDoc = returnStringList.DataList[2].Data;
                        }
                        else
                        {
                            throw new Exception(returnStringList.DataList[1].Data);
                        }
                    }
                    catch (Exception ex)
                    {
                        SetLog("SaveDocument", OfficeSpaceId, ObjectId, NDB.OutputFileID, xDoc.OuterXml, "", "", ex.Message);
                    }
                }

                SetLog("InsertData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, "", "");

                try
                {
                    Transform(Connection, OfficeSpaceId, DatabaseName, TableName, NDB.NewItemID, User, true);
                }
                catch { }

                // return Ok(SetReturnStringList(new String[] { "OK", "เพิ่มข้อมูลเรียบร้อยแล้ว", Version, VersionDoc, NDB.NewItemID, NDB.OutputFileID }));
                string[] output = { "OK", "เพิ่มข้อมูลเรียบร้อยแล้ว", Version, VersionDoc, NDB.NewItemID, NDB.OutputFileID };
                return output;
            }
            else
            {
                SetLog("InsertData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, "", NDB.ErrorMsg);
                string[] output = { "Error", NDB.ErrorMsg };
                return output;
                // return Ok(SetReturnStringList(new String[] { "Error", NDB.ErrorMsg, "", "", "", "" }));
            }
        }

        public string[] UpdateDataDoc(String Connection, String OfficeSpaceId, String DatabaseName, String TableName, String NColumns_String, String NWheres_String, String strDOC, String User)
        {
            _UserName = User;

            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                SetLog("UpdateData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, NWheres_String, "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง");

                string[] output = { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง" };
                return output;
                //  return Ok(SetReturnStringList(new String[] { "Error", "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง", "", "" }));
            }

            NextwaverDB.NColumns NCS = new NextwaverDB.NColumns();

            NCS.strXML = strDOC;

            if (!NCS.ImportString(NColumns_String))
            {
                SetLog("UpdateData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, NWheres_String, NCS.ErrorMSG);

                //  return Ok(SetReturnStringList(new String[] { "Error", NCS.ErrorMSG, "", "" }));

                string[] strList = { "Error", NCS.ErrorMSG };
                return strList;
            }

            NextwaverDB.NWheres NWS = new NextwaverDB.NWheres();

            if (!NWS.ImportString(NWheres_String))
            {
                SetLog("UpdateData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, NWheres_String, NCS.ErrorMSG);

                // return Ok(SetReturnStringList(new String[] { "Error", NWS.ErrorMSG, "", "" }));

                string[] strList = { "Error", NWS.ErrorMSG };
                return strList;
            }

            NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, _PathProvider.MapPath(""));

            if (NDB.update(DatabaseName, TableName, NCS, NWS))
            {
                String Version = "", VersionDoc = "";

                NextwaverDB.NOutputXMLs NOPX_Update = NDB.NOPXMLS_Update;

                for (Int32 i = 0; i < NOPX_Update._Count; i++)
                {
                    NextwaverDB.NOutputXML NOPX = NOPX_Update.get(i);

                    String FID = NOPX.ObjectID;

                    XmlDocument xDoc = new XmlDocument();

                    xDoc.LoadXml(NOPX.strXML);

                    String ObjectId = "DB-" + DatabaseName + "$TB-" + TableName;

                    try
                    {
                        ReturnStringList returnStringList = _SaveDocumentTest(OfficeSpaceId, ObjectId, FID, xDoc.OuterXml, DatabaseName, TableName);
                    }
                    catch (Exception ex)
                    {
                        SetLog("SaveDocument", OfficeSpaceId, ObjectId, FID, xDoc.OuterXml, "", "", ex.Message);
                    }
                }

                NextwaverDB.NOutputXMLs NOPX_Doc = NDB.NOPXMLS_Doc;

                for (Int32 i = 0; i < NOPX_Doc._Count; i++)
                {
                    NextwaverDB.NOutputXML NOPX = NOPX_Doc.get(i);

                    String FID = NOPX.ObjectID;

                    XmlDocument xDoc = new XmlDocument();

                    xDoc.LoadXml(NOPX.strXML);

                    String ObjectId = "DB-" + DatabaseName + "$TB-" + TableName + "$DOC";

                    try
                    {
                        ReturnStringList returnStringList = _SaveDocumentTest(OfficeSpaceId, ObjectId, FID, xDoc.OuterXml, DatabaseName, TableName);
                    }
                    catch (Exception ex)
                    {
                        SetLog("SaveDocument", OfficeSpaceId, ObjectId, FID, xDoc.OuterXml, "", "", ex.Message);
                    }
                }

                try
                {
                    Transform(Connection, OfficeSpaceId, DatabaseName, TableName, NWS.Get("ID").Value, User, true);
                }
                catch
                { }

                SetLog("UpdateData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, NWheres_String, "");

                //  return Ok(SetReturnStringList(new String[] { "OK", NDB.OutputMsg, Version, VersionDoc }));
                string[] output = { "OK", NDB.OutputMsg, Version, VersionDoc };
                return output;
            }
            else
            {
                SetLog("UpdateData", OfficeSpaceId, DatabaseName, TableName, strDOC, NColumns_String, NWheres_String, NDB.ErrorMsg);

                // return Ok(SetReturnStringList(new String[] { "Error", NDB.ErrorMsg, "", "" }));
                string[] output = { "Error", NDB.ErrorMsg };
                return output;
            }
        }
        private String GetPathStoreTest()
        {
            return _PathProvider.MapPath("Store");
        }
        private ReturnStringList _SaveDocumentTest(String OfficeSpaceId, String ObjectId, String ItemId, String strDocument, String DatabaseName, String TableName)
        {
            if (DatabaseName == "Doc" && TableName == "SubPage")
            {
                XmlDocument xFileSystem = new XmlDocument();

                xFileSystem.Load(_PathProvider.MapPath("FileSystem.xml"));

                String PathStore = GetPathStoreTest();

                Int32 iID = Int32.Parse(ItemId);

                String ItemIdFolder = Gobals.Methods.GenItemFile(iID);

                String FolderId = xFileSystem.SelectSingleNode("//Item[@Min<=" + Int32.Parse(ItemId) + "][@Max>=" + Int32.Parse(ItemId) + "]").Attributes["ID"].Value;

                String Version;

                try
                {
                    // E:\THODSAPON\REST\WorkSpace\WorkSpace\Store\OF.0001\database\File\SubPage\DB-Doc$TB-SubPage$DOC
                    String[] DirList = Directory.GetDirectories(PathStore + @"\" + OfficeSpaceId + @"\database\File\SubPage\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder);

                    String LastVersionFolder = Gobals.Methods.GenFolderId(DirList.Length);

                    String[] filList = Directory.GetFiles(PathStore + @"\" + OfficeSpaceId + @"\database\File\SubPage\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + LastVersionFolder);

                    Version = "" + (((DirList.Length - 1) * 2000) + (filList.Length + 1));
                }
                catch
                {
                    Version = "1";
                }

                Gobals.Sockets.TCP_Client TCPC = new Gobals.Sockets.TCP_Client();

                String Server_IP = GetIP();

                String FolderVersion = xFileSystem.SelectSingleNode("//Item[@Min<=" + Int32.Parse(Version) + "][@Max>=" + Int32.Parse(Version) + "]").Attributes["ID"].Value;

                String FileVersionName = Gobals.Methods.GenItemFile(Int32.Parse(Version));

                // database\File\SubPage
                //เริ่มสร้าง ROOT PATH
                if (!Directory.Exists(PathStore))
                    Directory.CreateDirectory(PathStore);

                if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId))
                    Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId);

                if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\database\File\SubPage"))
                    Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\database\File\SubPage");

                if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\database\File\SubPage\" + ObjectId))
                    Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\database\File\SubPage\" + ObjectId);
                //จบการสร้าง ROOT PATH

                //เริ่มสร้าง Version Control
                if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\database\File\SubPage\" + ObjectId + @"\LV"))
                    Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\database\File\SubPage\" + ObjectId + @"\LV");

                if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\database\File\SubPage\" + ObjectId + @"\CV"))
                    Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\database\File\SubPage\" + ObjectId + @"\CV");
                //จบการสร้าง Version Control

                //เริ่มสร้าง Folder ID
                if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\database\File\SubPage\" + ObjectId + @"\LV\" + FolderId))
                    Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\database\File\SubPage\" + ObjectId + @"\LV\" + FolderId);

                if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\database\File\SubPage\" + ObjectId + @"\CV\" + FolderId))
                    Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\database\File\SubPage\" + ObjectId + @"\CV\" + FolderId);
                //จบการสร้าง Folder ID

                //เริ่มสร้าง Item Folder ID      
                if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\database\File\SubPage\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder))
                    Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\database\File\SubPage\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder);
                //จบการสร้าง Item Folder ID

                //เริ่มสร้าง Item Folder Version ID       
                if (!Directory.Exists(PathStore + @"\" + OfficeSpaceId + @"\database\File\SubPage\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + FolderVersion))
                    Directory.CreateDirectory(PathStore + @"\" + OfficeSpaceId + @"\database\File\SubPage\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + FolderVersion);
                //จบการสร้าง Item Folder Version ID

                String SaveFileLastVersion = PathStore + @"\" + OfficeSpaceId + @"\database\File\SubPage\" + ObjectId + @"\LV\" + FolderId + @"\" + ItemIdFolder + ".xml";

                String SaveFileControlVersion = PathStore + @"\" + OfficeSpaceId + @"\database\File\SubPage\" + ObjectId + @"\CV\" + FolderId + @"\" + ItemIdFolder + @"\" + FolderVersion + @"\" + FileVersionName + ".diff";

                if (Version != "1")
                {
                    String Document_LastVersion = System.IO.File.ReadAllText(SaveFileLastVersion);

                    String docHas, strDIff = "", msgError;

                    Boolean bError = false;

                    Gobals.ControlVersion.CreateDiff(Document_LastVersion, strDocument, _PathProvider.MapPath("Temp"), out docHas, out strDIff, out bError, out msgError);

                    if (strDIff == "")
                    {
                        return SetReturnStringList(new String[] { "Error", "ไม่มีการแก้ไขข้อมูล", "" });
                    }

                    String strTemp = "<?xml version=\"1.0\" encoding=\"windows-874\"?><xd:xmldiff version=\"1.0\" srcDocHash=\"" + docHas + "\" options=\"None\" fragments=\"no\" xmlns:xd=\"http://schemas.microsoft.com/xmltools/2002/xmldiff\" />";

                    strTemp = strTemp.Replace(" ", "");

                    if (strDIff.Replace(" ", "") == strTemp)
                    {
                        return SetReturnStringList(new String[] { "Error", "ไม่มีการแก้ไขข้อมูล", "" });
                    }

                    XmlDocument xTempp = new XmlDocument();

                    xTempp.LoadXml(strDocument);

                    xTempp.Save(SaveFileLastVersion);

                    Gobals.Methods.SaveFile(SaveFileControlVersion, strDIff);

                    return SetReturnStringList(new String[] { "", "", Version });
                }
                else
                {
                    //บันทึกข้อมูล Version สุดท้าย
                    XmlDocument xTempp = new XmlDocument();

                    xTempp.LoadXml(strDocument);

                    xTempp.Save(SaveFileLastVersion);
                    // จบการบันทึก        

                    //บันทึกข้อมูล Control Version สุดท้าย
                    xTempp.Save(SaveFileControlVersion);
                    // จบการบันทึก

                    return SetReturnStringList(new String[] { "", "", Version });
                }
            }
            else
            {
                return SetReturnStringList(new String[] { "OK", "", "" });
            }
        }

        private DataTable SelectAllColumnByWhereTest(string Connection, string OfficeSpaceId, string DatabaseName, string TableName, string NWheres_String, string User)
        {
            UserName = User;
            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                ErrorMSG = "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง";
                SetLog("SelectAllColumnByWhere", OfficeSpaceId, DatabaseName, TableName, "", "", NWheres_String, ErrorMSG);
                return null;
            }
            NextwaverDB.NWheres NWS = new NextwaverDB.NWheres();
            if (!NWS.ImportString(NWheres_String))
            {
                SetLog("SelectAllColumnByWhere", OfficeSpaceId, DatabaseName, TableName, "", "", NWheres_String, "NWheres:" + NWS.ErrorMSG);
                return null;
            }
            NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, _PathProvider.MapPath(""));
            DataTable dt = NDB.select(DatabaseName, TableName, NWS);
            if (dt == null)
                SetLog("SelectAllColumnByWhere", OfficeSpaceId, DatabaseName, TableName, "", "", NWheres_String, NDB.ErrorMsg);
            else
                SetLog("SelectAllColumnByWhere", OfficeSpaceId, DatabaseName, TableName, "", "", NWheres_String, "");
            return dt;
        }
        private DataTable SelectAllTest(string Connection, string OfficeSpaceId, string DatabaseName, string TableName, string User)
        {
            UserName = User;
            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                ErrorMSG = "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง";
                return null;
            }
            NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, _PathProvider.MapPath(""));
            DataTable dt = NDB.select(DatabaseName, TableName);
            if (dt == null)
                SetLog("SelectAll", OfficeSpaceId, DatabaseName, TableName, "", "", "", NDB.ErrorMsg);
            else
                SetLog("SelectAll", OfficeSpaceId, DatabaseName, TableName, "", "", "", "");
            return dt;
        }

        private string SelectLastDocumentTest(string Connection, string OfficeSpaceId, string DatabaseName, string TableName, int ItemId, string User)
        {
            UserName = User;
            if (Connection != Startup.Configuration.GetSection("WorkSpaceConfig").GetSection("Connection").Value)
            {
                ErrorMSG = "ไม่สามารถติดต่อฐานข้อมูลได้เนื่องจาก Connection ไม่ถูกต้อง";
                SetLog("SelectLastDocument", OfficeSpaceId, DatabaseName, TableName, "", "", "", "[ID=" + ItemId + "]" + ErrorMSG);
                return "";
            }

            NextwaverDB.NDB NDB = new NextwaverDB.NDB(OfficeSpaceId, _PathProvider.MapPath(""));
            string OutputXML = "";
            if (NDB.selectLastDoc(DatabaseName, TableName, ItemId, out OutputXML))
            {
                ErrorMSG = "";
                SetLog("SelectLastDocument", OfficeSpaceId, DatabaseName, TableName, "", "", "", "");
                return OutputXML;
            }
            else
            {
                ErrorMSG = NDB.ErrorMsg;
                SetLog("SelectLastDocument", OfficeSpaceId, DatabaseName, TableName, "", "", "", "[ID=" + ItemId + "]" + ErrorMSG);
                return "";
            }
        }

        #endregion

        #region Method
  
        private void AddValueColumn(ref XmlDocument xDoc, ref NextwaverDB.NColumns NCS, String RootPath, String ColumnName, String Value)
        {
            NCS.Add(new NextwaverDB.NColumn(ColumnName, Value));
            AddDataXmlNode(ref xDoc, RootPath + "/Item[@Name='" + ColumnName + "']", Value);
        }
        private void updateDocByID(Int32 DocID, XmlDocument xDocNew, NextwaverDB.NColumns NCS, String TableName)
        {
            try
            {
                xDoc = new XmlDocument();
                xDoc.LoadXml(xDocNew.OuterXml);

                string strDoc = xDoc.OuterXml;
                NWS = new NextwaverDB.NWheres();
                NWS.Add(new NextwaverDB.NWhere("ID", Convert.ToString(DocID)));

                UpdateDataDoc(_Connection, _OfficeSpaceId, _DatabaseName, TableName, NCS.ExportString(), NWS.ExportString(), strDoc, "system");
                UpdateDataDoc(_Connection, _OfficeSpaceId, "File", TableName, NCS.ExportString(), NWS.ExportString(), strDoc, "system");
            }
            catch (Exception ex)
            {
                Console.WriteLine("updateDocByID : " + ex.Message);
            }
        }
        public void CreateLog(String result)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(_PathProvider.MapPath("Error.txt"), true))
            {
                file.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " : " + result);
            }
        }

        public void AddItemFile(ref XmlDocument xDoc, String StartNoode, String FileName, String FilePath, String FilePathLV, String FileExtension)
        {
            XmlNode nodeDataGrid = xDoc.SelectSingleNode(StartNoode);
            XmlElement xElem = xDoc.CreateElement("File");
            xElem.Attributes.Append(XmlAttribute("FileName", FileName, ref xDoc));
            xElem.Attributes.Append(XmlAttribute("FilePath", FilePath, ref xDoc));
            xElem.Attributes.Append(XmlAttribute("FilePathLV", FilePathLV, ref xDoc));
            xElem.Attributes.Append(XmlAttribute("FileExtension", FileExtension, ref xDoc));
            nodeDataGrid.AppendChild(xElem);
        }

        public void AddXmlCDataSection(ref XmlDocument xDoc, String FilePath)
        {
            XmlNode xnode = xDoc.SelectSingleNode(@"FileList/File[@FilePath='" + FilePath + "']");
            XmlCDataSection cdata = xDoc.CreateCDataSection(System.IO.File.ReadAllText(FilePath));

            xnode.InnerXml = cdata.OuterXml;
        }
        private void WriteFile_LV(String FilePath, XmlNode xNode)
        {
            try
            {
                System.IO.File.WriteAllText(_PathProvider.MapPath(FilePath), String.Empty);
                using (System.IO.StreamWriter fileDiff = new System.IO.StreamWriter(_PathProvider.MapPath(FilePath), true))
                {
                    //  XmlNode node = doc.DocumentElement.SelectSingleNode(@"/Books/Book");
                    XmlNode childNode = xNode.ChildNodes[0];
                    if (childNode is XmlCDataSection)
                    {
                        XmlCDataSection cdataSection = childNode as XmlCDataSection;
                        fileDiff.Write(FormatXml(cdataSection.Value));
                    }
                }
            }
            catch (Exception ex)
            {
                CreateLog("WriteFile_LV >>>> " + ex.Message);
            }
        }

        private void WriteFile_CV(String FilePath, String FilePathFull, String FilePathLV, String FileName, XmlNode xNode)
        {
            try
            {
                // Get String Diff
                String Diff = "";
                XmlNode childNode = xNode.ChildNodes[0];
                if (childNode is XmlCDataSection)
                {
                    XmlCDataSection cdataSection = childNode as XmlCDataSection;
                    Diff = cdataSection.Value;
                }

                xDoc = new XmlDocument();
                if (Convert.ToInt32(FileName.Split('.')[0]) > 1)
                {
                    // DiffPatch_LastVersion then Version > 1
                    // Store\OF.0001\database\File\SubPage\DB-Doc$TB-SubPage\CV\0001\0000001\0001\0000011.diff

                    // PatchDiff LastVersion
                    String strDoc = DiffPatch_LastVersion(FilePathLV, Diff);
                    System.IO.File.WriteAllText(FilePathLV, String.Empty);
                    using (System.IO.StreamWriter fileDiff = new System.IO.StreamWriter(FilePathLV, true))
                    {
                        fileDiff.WriteLine(FormatXml(strDoc));
                    }

                    // Create New Diff
                    System.IO.File.WriteAllText(FilePathFull, String.Empty);
                    using (System.IO.StreamWriter fileDiff = new System.IO.StreamWriter(FilePathFull, true))
                    {
                        fileDiff.WriteLine(FormatXml(Diff));
                    }

                    // PatchDiff LastVersion in Document Folder
                    if (!FilePath.Contains("DB-Doc$TB-SubPage$DOC"))
                    {
                        // Store\OF.0001\database\File\SubPage\DB-Doc$TB-SubPage\CV\0001\0000001\0001\0000011.diff
                        // Store\OF.0001\database\Doc\SubPage\0001.xml

                        String PathSubPage = _PathProvider.MapPath(@"Store\OF.0001\database\Doc\SubPage\" + Convert.ToInt32(FilePath.Split('\\')[8]).ToString("0000") + ".xml");
                        strDoc = DiffPatch_LastVersion(PathSubPage, Diff);
                        if (strDoc == "")
                        {
                            CreateLog("########## START ###########");
                            CreateLog("PathSubPage : " + PathSubPage);
                            CreateLog(">>>>>> Result : ");
                            CreateLog(strDoc);
                            CreateLog("########## END ###########");
                        }

                        System.IO.File.WriteAllText(PathSubPage, String.Empty);
                        using (System.IO.StreamWriter fileDiff = new System.IO.StreamWriter(PathSubPage, true))
                        {
                            fileDiff.WriteLine(FormatXml(strDoc));
                        }


                    }
                }
                else
                {
                    // Create Last Version 
                    //  System.IO.File.WriteAllText(_PathProvider.MapPath(FilePathLV), String.Empty);
                    //  using (System.IO.StreamWriter fileDiff = new System.IO.StreamWriter(_PathProvider.MapPath(FilePathLV), true))
                    //  {
                    //      fileDiff.WriteLine(FormatXml(Diff));
                    //  }
                    //
                    // Create ControllVersion First
                    using (System.IO.StreamWriter fileDiff = new System.IO.StreamWriter(_PathProvider.MapPath(FilePath), true))
                    {
                        fileDiff.WriteLine(FormatXml(Diff));
                    }
                }
            }
            catch (Exception ex)
            {
                CreateLog("WriteFile_CV >>>> " + ex.Message);
            }
        }


        private String DiffPatch_LastVersion(String PathDocLastVersion, String strDiff)
        {
            try
            {
                String newVersionDocument = "", MsgError = "";
                Boolean bError;

                // Get Document Last Version

                String DocumentLastVersion = System.IO.File.ReadAllText(_PathProvider.MapPath(PathDocLastVersion));

                // DiffPatch Last Version
                Gobals.ControlVersion.PatchXML(DocumentLastVersion, strDiff, _PathProvider.MapPath("Temp"), out newVersionDocument, out bError, out MsgError);

                return newVersionDocument;
            }
            catch (Exception ex)
            {
                CreateLog("DiffPatch_LastVersion >>>> " + ex.Message);
                return ex.Message;
            }
        }

        public XmlDocument GetFileSystemInfoList(ref XmlDocument _dirFileDiff, string RootPath)
        {
            try
            {
                XmlElement nodeElem = XmlElement("folder", new DirectoryInfo(RootPath).Name, ref _dirFileDiff);
                _dirFileDiff.AppendChild(AddElements(nodeElem, RootPath, ref _dirFileDiff));
            }
            catch (Exception ex)
            {
                _dirFileDiff.AppendChild(XmlElement("error", ex.Message, ref _dirFileDiff));
                return _dirFileDiff;
            }
            return _dirFileDiff;
        }
        private XmlElement AddElements(XmlElement startNode, string Folder, ref XmlDocument xmlDoc)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(Folder);
                DirectoryInfo[] subDirs = dir.GetDirectories();
                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo fi in files)
                {
                    XmlElement fileElem = XmlElement("file", fi.Name, ref xmlDoc);
                    fileElem.Attributes.Append(XmlAttribute("Extension", fi.Extension, ref xmlDoc));
                    fileElem.Attributes.Append(XmlAttribute("FilePath", fi.FullName.Replace(_PathProvider.MapPath("") + "\\", ""), ref xmlDoc));
                    fileElem.Attributes.Append(XmlAttribute("Hidden", ((fi.Attributes & FileAttributes.Hidden) != 0) ? "Y" : "N", ref xmlDoc));
                    fileElem.Attributes.Append(XmlAttribute("Archive", ((fi.Attributes & FileAttributes.Archive) != 0) ? "Y" : "N", ref xmlDoc));
                    fileElem.Attributes.Append(XmlAttribute("System", ((fi.Attributes & FileAttributes.System) != 0) ? "Y" : "N", ref xmlDoc));
                    fileElem.Attributes.Append(XmlAttribute("ReadOnly", ((fi.Attributes & FileAttributes.ReadOnly) != 0) ? "Y" : "N", ref xmlDoc));
                    startNode.AppendChild(fileElem);
                }
                foreach (DirectoryInfo sd in subDirs)
                {
                    XmlElement folderElem = XmlElement("folder", sd.Name, ref xmlDoc);
                    folderElem.Attributes.Append(XmlAttribute("FilePath", sd.FullName.Replace(_PathProvider.MapPath("") + "\\", ""), ref xmlDoc));
                    folderElem.Attributes.Append(XmlAttribute("Hidden", ((sd.Attributes & FileAttributes.Hidden) != 0) ? "Y" : "N", ref xmlDoc));
                    folderElem.Attributes.Append(XmlAttribute("System", ((sd.Attributes & FileAttributes.System) != 0) ? "Y" : "N", ref xmlDoc));
                    folderElem.Attributes.Append(XmlAttribute("ReadOnly", ((sd.Attributes & FileAttributes.ReadOnly) != 0) ? "Y" : "N", ref xmlDoc));
                    startNode.AppendChild(AddElements(folderElem, sd.FullName, ref xmlDoc));
                }
                return startNode;
            }
            catch (Exception ex)
            {
                return XmlElement("error", ex.Message, ref xmlDoc);
            }
        }
        private XmlAttribute XmlAttribute(string attributeName, string attributeValue, ref XmlDocument xmlDoc)
        {
            XmlAttribute xmlAttrib = xmlDoc.CreateAttribute(attributeName);
            xmlAttrib.Value = FilterXMLString(attributeValue);
            return xmlAttrib;
        }
        private XmlElement XmlElement(string elementName, string elementValue, ref XmlDocument xmlDoc)
        {
            XmlElement xmlElement = xmlDoc.CreateElement(elementName);
            xmlElement.Attributes.Append(XmlAttribute("name", FilterXMLString(elementValue), ref xmlDoc));
            return xmlElement;
        }
        private string FilterXMLString(string inputString)
        {
            string returnString = inputString;
            if (inputString.IndexOf("&") > 0)
            {
                returnString = inputString.Replace("&", "&amp;");
            }
            if (inputString.IndexOf("'") > 0)
            {
                returnString = inputString.Replace("'", "&apos;");
            }
            return returnString;
        }
        public String CallAPI(String url, Method MethodType, String Text)
        {
            var client = new RestClient(url);
            var request = new RestRequest();
            var uri = new Uri(url);
            request = new RestRequest(uri, MethodType);

            if (MethodType == Method.Post)
            {
                byte[] Latin1Bytes = Encoding.UTF8.GetBytes(Text);
                request.AddFile("file", Latin1Bytes, "File");
                //request.AddFileBytes("file", Latin1Bytes, "File");
            }

            //request.AddHeader("Content-Type", "application/xml");

            RestResponse response = client.Execute(request);
            String ResultString = response.Content;
            return ResultString;
        }
        public static void AddDataXmlNode(ref XmlDocument xDoc, string xPath, string sValue)
        {
            XmlNode nodeControl = xDoc.SelectSingleNode(xPath);
            nodeControl.Attributes["Value"].Value = sValue;
        }
        public static void AddDataXmlNode(string xPath, string sValue)
        {
            XmlNode nodeControl = xDoc.SelectSingleNode(xPath);
            nodeControl.Attributes["Value"].Value = sValue;
        }
        public XmlDocument AddDataXmlNode(XmlDocument xDoc, string xPath, string sValue)
        {
            XmlNode nodeControl = xDoc.SelectSingleNode(xPath);
            nodeControl.Attributes["Value"].Value = sValue;
            return xDoc;
        }

        private static void XmlAddAttribute(XmlDocument XmlDoc, XmlNode ParentElement, String AttrName, String AttrValue)
        {
            XmlAttribute NewAttr = XmlDoc.CreateAttribute(AttrName);
            NewAttr.Value = AttrValue;
            ParentElement.Attributes.Append(NewAttr);
        }
        private String GetTimestamp()
        {
            TimeSpan span = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());
            return span.Ticks.ToString();
        }
        public string FormatXml(string inputXml)
        {
            XmlDocument document = new XmlDocument();
            document.Load(new StringReader(inputXml));

            StringBuilder builder = new StringBuilder();
            using (XmlTextWriter writer = new XmlTextWriter(new StringWriter(builder)))
            {
                writer.Formatting = System.Xml.Formatting.Indented;
                document.Save(writer);
            }

            return builder.ToString().Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", "").TrimStart();
        }
        #endregion

        #region ZIP
        [HttpGet("ZipFiles")]
        public IActionResult ZipFiles()
        {
            try
            {
                string startPath = _PathProvider.MapPath("Store/OF.0001/database/CompanyCarsDocker-master");
                string zipPath = _PathProvider.MapPath("Store/OF.0001/database/20.zip");
                ZipFile.CreateFromDirectory(startPath, zipPath);

                return Ok(SetReturnStringList(new String[] { "OK", "ZIP FILE SUCCESS" }));
            }
            catch (Exception ex)
            {

                return Ok(SetReturnStringList(new String[] { "ERROR", ex.Message }));

            }
        }

        [HttpGet("ExtractFile")]
        public IActionResult ExtractFile()
        {
            try
            {
                string zipPath = _PathProvider.MapPath("Store/OF.0001/database/20.zip");
                string extractPath = _PathProvider.MapPath("Store/OF.0001");

                ZipFile.ExtractToDirectory(zipPath, extractPath);
                return Ok(SetReturnStringList(new String[] { "OK", "Extract Files SUCCESS" }));
            }
            catch (Exception ex)
            {
                return Ok(SetReturnStringList(new String[] { "ERROR", ex.Message }));
            }
        }
        #endregion

        #region DataEncrypterDecrypter
        public string Encrypt(string input)
        {
            byte[] inputArray = UTF8Encoding.UTF8.GetBytes(input);
            TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider();
            tripleDES.Key = UTF8Encoding.UTF8.GetBytes(EncryptKey);
            tripleDES.Mode = CipherMode.ECB;
            tripleDES.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = tripleDES.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);
            tripleDES.Clear();
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }
        public string Decrypt(string input)
        {
            string pattern = " ";
            string replace = "+";
            input = Regex.Replace(input, pattern, replace);

            byte[] inputArray = Convert.FromBase64String(input);
            TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider();
            tripleDES.Key = UTF8Encoding.UTF8.GetBytes(EncryptKey);
            tripleDES.Mode = CipherMode.ECB;
            tripleDES.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = tripleDES.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);
            tripleDES.Clear();
            return UTF8Encoding.UTF8.GetString(resultArray);
        }
        #endregion

    }
}
