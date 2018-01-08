/*
 *  auto: likk
 *  data: 2018/01/08
 *  version: 1.0.0
 *  desc: 解析cocos studio的文件目录，解析生成ccs文件
 */

using System;
using System.Linq;
using System.IO;
using System.Xml;

namespace generate_ccs
{
    class Program
    {
        //const string path = "E:\\tank\\jishu\\tank2\\tank2_ui\\cocosstudio";
        //const string pathCcs = "E:\\tank\\jishu\\tank2\\tank2_ui";
        const string path = ".\\cocosstudio";
        const string pathCcs = ".\\";
        static string projectName = "cocos_studio";
        
        //文件格式后缀
        static string[] fileEnd_project = new string[] { "csd" };
        static string[] fileEnd_image = new string[] { "bmp", "jpg", "png", "tiff", "gif", "pcx", "tga", "exif", "fpx", "svg", "psd", "cdr", "pcd", "dxf", "ufo", "eps", "ai", "raw", "WMF" };
        static string[] fileEnd_ttf = new string[] { "ttf", "TTF" };
        static string[] fileEnd_fnt = new string[] { "fnt" };
        static string[] fileEnd_plist = new string[] { "csi" };
        static string[] fileEnd_partical = new string[] { "plist" };
        static string[] fileEnd_except = new string[] { "udf" }; //不进行解析的文件

        enum FileType
        {
            Project = 0,  //csd文件
            PlistInfo, //csi文件
            Image,
            TTF,
            Fnt,
            PlistParticleFile, // plist后缀，特效
            Except, //排除的文件，不进行检查
            Unknow, //未能识别的文件
            Count,
        }

        static void Main(string[] args)
        {
            XmlDocument ccs = new XmlDocument();
            Console.SetBufferSize(120, 5000);
            Console.SetWindowSize(120, 40);
            string filePath = pathCcs + "\\" + projectName + ".ccs";
            try
            {
                ccs.Load(filePath);
            }
            catch (Exception)
            {
                Console.WriteLine("不存在CCS文件:" + filePath);
            }
            XmlNode rootFolderNode = GenerateCcs(ref ccs);
            CheckFiles(ccs, path, rootFolderNode);
            //XmlWriter xmlWriter = XmlWriter.Create(filePath);
            //ccs.WriteContentTo(xmlWriter);
            //xmlWriter.Flush();
            //xmlWriter.Dispose();
            ccs.Save(filePath);

            Console.WriteLine("解析完毕");
            Console.ReadKey();
        }

        static XmlNode GenerateCcs(ref XmlDocument ccs)
        {
            XmlNode nodeSolution = ccs.SelectSingleNode("Solution");

            XmlNode RootFolderNode;
            if (null == nodeSolution)
            {
                nodeSolution = ccs.CreateElement("Solution");
                ccs.AppendChild(nodeSolution);

                XmlElement propertyNode = ccs.CreateElement("PropertyGroup");
                propertyNode.SetAttribute("Name", projectName);
                propertyNode.SetAttribute("Version",  "3.10.0.0");
                propertyNode.SetAttribute("Type",  "CocosStudio");
                nodeSolution.AppendChild(propertyNode);

                XmlElement SolutionFolderNode = ccs.CreateElement("SolutionFolder");
                nodeSolution.AppendChild(SolutionFolderNode);

                XmlElement GroupNode = ccs.CreateElement("Group");
                GroupNode.SetAttribute("ctype",  "ResourceGroup");
                SolutionFolderNode.AppendChild(GroupNode);

                RootFolderNode = ccs.CreateElement("RootFolder");
                (RootFolderNode as XmlElement).SetAttribute("Name", ".");
                GroupNode.AppendChild(RootFolderNode);
            } else
            {
                RootFolderNode = ccs.SelectSingleNode("Solution/SolutionFolder/Group/RootFolder");
                RootFolderNode.RemoveAll();
                (RootFolderNode as XmlElement).SetAttribute("Name", ".");
            }
            return RootFolderNode;
        }

        static void CheckFiles(XmlDocument ccs,  string path,  XmlNode rootNode)
        {
            if (null == ccs || null == rootNode || string.IsNullOrEmpty(path))
            {
                Console.WriteLine("参数错误");
                return;
            }
            DirectoryInfo directory = new DirectoryInfo(path);
            if (!directory.Exists)
            {
                WriteError("读入目录错误:" + path);
                return;
            }
            Console.WriteLine("读取目录:" + directory.FullName);
            FileSystemInfo[] fileInfos = directory.GetFileSystemInfos();
            for (int i = 0; i < fileInfos.Length; i++)
            {
                FileInfo file = fileInfos[i] as FileInfo;
                string fileName = fileInfos[i].Name;
                if (null == file)
                {
                    //如果是文件夹
                    XmlNode folderNode = ccs.CreateElement("Folder");
                    (folderNode as XmlElement).SetAttribute("Name", fileName);
                    rootNode.AppendChild(folderNode);
                    CheckFiles(ccs,  path + "/" + fileInfos[i].Name,  folderNode);
                }
                else
                {
                    FileType fileType = CheckFileType(fileName);
                    switch (fileType)
                    {
                        case FileType.Project:
                        case FileType.PlistInfo:
                            //如果是csd工程文件，需要读取文件类型
                            string projectType = GetProjectType(fileType, fileInfos[i].FullName);
                            XmlElement ProjectNode = ccs.CreateElement(fileType.ToString());
                            ProjectNode.SetAttribute("Name", fileName);
                            ProjectNode.SetAttribute("Type", projectType);
                            rootNode.AppendChild(ProjectNode);
                            break;
                        case FileType.Image:
                        case FileType.TTF:
                        case FileType.Fnt:
                        case FileType.PlistParticleFile:
                            XmlElement fileNode = ccs.CreateElement(fileType.ToString());
                            fileNode.SetAttribute("Name", fileName);
                            rootNode.AppendChild(fileNode);
                            break;
                        case FileType.Unknow:
                        case FileType.Except:
                        case FileType.Count:
                        default:
                            break;
                    }
                }
            }

            //XmlNodeList csdList = rootNode.SelectNodes("Project");
            //for (int i = 0; i < csdList.Count; i++)
            //{
            //    rootNode.RemoveChild(csdList[i]);
            //}
            //for (int i = 0; i < csdList.Count; i++)
            //{
            //    rootNode.AppendChild(csdList[i]);
            //}
        }

        /// <summary>
        /// 解析文件类型，获取ccs中的文件tpye
        /// </summary>
        /// <param name="name">文件名，不包含路径</param>
        /// <returns >FileType类型的枚举</returns>
        static FileType CheckFileType(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                WriteError("1类型错误 文件名异常:" + name);
                return FileType.Unknow;
            }
            string[] strs = name.Split('.');
            if (strs.Length < 2)
            {
                WriteError("检查文件类型错误 文件名异常:" + name);
                return FileType.Unknow;
            }
            string strType = strs[strs.Length - 1];
            if (fileEnd_project.Contains(strType))
            {
                return FileType.Project;
            }
            else if (fileEnd_plist.Contains(strType))
            {
                return FileType.PlistInfo;
            }
            else if (fileEnd_image.Contains(strType))
            {
                return FileType.Image;
            }
            else if (fileEnd_ttf.Contains(strType))
            {
                return FileType.TTF;
            }
            else if (fileEnd_fnt.Contains(strType))
            {
                return FileType.Fnt;
            }
            else if (fileEnd_partical.Contains(strType))
            {
                return FileType.PlistParticleFile;
            }
            else if (fileEnd_except.Contains(strType))
            {
                return FileType.Except;
            }
            else
            {
                WriteError(string.Format("检查文件类型错误 不识别的文件后缀:{0} 请确认文件 {1},或者与开发人员联系", strType, name));
                return FileType.Unknow;
            }
        }

        /// <summary>
        /// 解析csi，csd工程文件，获取类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        static string GetProjectType(FileType type, string path)
        {
            string csdType = "";
            try
            {
                XmlDocument csd = new XmlDocument();
                csd.Load(path);
                XmlElement propertyGroupNode = null;
                if (type == FileType.Project)
                {
                    propertyGroupNode = csd.SelectSingleNode("GameFile/PropertyGroup") as XmlElement;
                }
                else if ( type == FileType.PlistInfo)
                {
                    propertyGroupNode = csd.SelectSingleNode("PlistInfoProjectFile/PropertyGroup") as XmlElement;
                }
                if (null !=propertyGroupNode)
                {
                    csdType = propertyGroupNode.GetAttribute("Type");
                }
            }
            catch (Exception)
            {
                WriteError(string.Format("解析文件{0}, 请联系开发人员", path));
            }
            return csdType;
        }

        static void WriteError(string error)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(string.Format(error));
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
