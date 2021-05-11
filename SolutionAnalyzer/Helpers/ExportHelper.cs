using System.Collections.Generic;
using System.Text;
using System.Xml;

using SolutionAnalyzer.CommonData;

namespace SolutionAnalyzer.Helpers
{
    internal class ExportHelper
    {
        public static void ExportXml(string fileName, string solutionFullName, IList<ProjectDataItem> projects)
        {
            //using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
            //using (StreamWriter sw = new StreamWriter(fileStream))
            System.Threading.Tasks.Task.Run(
                () =>
                    {
                        using (XmlTextWriter textWriter = new XmlTextWriter(fileName, Encoding.UTF8))
                        {
                            textWriter.Formatting = Formatting.Indented;
                            textWriter.Indentation = 2;


                            // Opens the document  
                            textWriter.WriteStartDocument();
                            // Write comments  
                            textWriter.WriteComment("Solution information from Solution Analyzer by Alex Nek");
                            textWriter.WriteStartElement("Solution");
                            textWriter.WriteAttributeString("Name", solutionFullName);
                            textWriter.WriteAttributeString("ProjectCount", projects.Count.ToString());
                            foreach (ProjectDataItem project in projects)
                            {
                                textWriter.WriteStartElement("Project");
                                //textWriter.WriteStartAttribute("abc");
                                //textWriter.WriteString("attrvalue-abc");
                                textWriter.WriteAttributeString("Name", project.Name);
                                if (project.Files != null)
                                {
                                    textWriter.WriteAttributeString("FilesCount", project.Files.ToString());
                                }
                                else
                                {
                                    textWriter.WriteAttributeString("FilesCount", "");
                                }

                                if (project.SummaryLines != null)
                                {
                                    textWriter.WriteAttributeString("ProjectLines", project.SummaryLines.ToString());
                                }
                                else
                                {
                                    textWriter.WriteAttributeString("ProjectLines", "");
                                }

                                foreach (FileDataItem file in project.CodeSourceFiles)
                                {
                                    textWriter.WriteStartElement("File");
                                    textWriter.WriteAttributeString("Name", file.Name);
                                    textWriter.WriteAttributeString("ClassCount", file.ClassCount.ToString());
                                    textWriter.WriteAttributeString("MemberCount", file.MemberCount.ToString());
                                    textWriter.WriteAttributeString("LineCount", file.LineCount.ToString());
                                    foreach (ClassMemberDataItem classMember in file.ClassMembers)
                                    {
                                        textWriter.WriteStartElement("Member");
                                        textWriter.WriteAttributeString("Name", classMember.Name);
                                        textWriter.WriteAttributeString("LineCount", classMember.LineCount.ToString());
                                        textWriter.WriteEndElement();
                                    }
                                    textWriter.WriteEndElement();
                                }
                                textWriter.WriteEndElement();
                            }
                            textWriter.WriteEndElement();
                            // Ends the document.  
                            textWriter.WriteEndDocument();
                        }
                    });
            
            
        }

        private static void WriteString(XmlTextWriter textWriter)
        {
            textWriter.WriteStartElement("Name", "");
            textWriter.WriteString("Student");
            textWriter.WriteEndElement();
        }
    }
}
