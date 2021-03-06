/*  This file is part of Chummer5a.
 *
 *  Chummer5a is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Chummer5a is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Chummer5a.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  You can obtain the full source code for Chummer5a at
 *  https://github.com/chummer5a/chummer5a
 */
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.XmlUnit.Builder;
using Org.XmlUnit.Diff;

namespace Chummer.Tests
{
    [TestClass]
    public static class AssemblyInitializer
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            Utils.IsUnitTest = true;
        }
    }

    [TestClass]
    public class ChummerTest
    {
        public ChummerTest()
        {
            string strPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "TestFiles");
            DirectoryInfo objPathInfo = new DirectoryInfo(strPath);//Assuming Test is your Folder
            foreach (DirectoryInfo objOldDir in objPathInfo.GetDirectories("TestRun-*"))
            {
                Directory.Delete(objOldDir.FullName, true);
            }
            TestPath = Path.Combine(strPath, "TestRun-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm", GlobalOptions.InvariantCultureInfo));
            TestPathInfo = Directory.CreateDirectory(TestPath);
            TestFiles = objPathInfo.GetFiles("*.chum5"); //Getting Text files
        }

        private string TestPath { get; }
        private DirectoryInfo TestPathInfo { get; }

        public FileInfo[] TestFiles { get; set; }


        // "B" will load before all other unit tests, this one tests basic startup (just starting Chummer without any characters
        [TestMethod]
        public void BasicStartup()
        {
            Debug.WriteLine("Unit test initialized for: BasicStartup()");
            frmChummerMain frmOldMainForm = Program.MainForm;
            // This lets the form be "shown" in unit tests (to actually have it show, ShowDialog() needs to be used)
            using (frmChummerMain frmTestForm = new frmChummerMain(true) { ShowInTaskbar = false })
            {
                Program.MainForm = frmTestForm; // Set program Main form to Unit test version
                frmTestForm.Show(); // Show the main form so that we know the UI can load in properly
            }
            Program.MainForm = frmOldMainForm;
        }


        // Test methods have a number in their name so that by default they execute in the order of fastest to slowest
        [TestMethod]
        public void Load1ThenSave()
        {
            Debug.WriteLine("Unit test initialized for: Load1ThenSave()");
            foreach (FileInfo objFileInfo in TestFiles)
            {
                string strDestination = Path.Combine(TestPathInfo.FullName, objFileInfo.Name);
                using (Character objCharacter = LoadCharacter(objFileInfo))
                    SaveCharacter(objCharacter, strDestination);
                using (Character _ = LoadCharacter(new FileInfo(strDestination)))
                { // Assert on failed load will already happen inside LoadCharacter
                }
            }
        }

        // Test methods have a number in their name so that by default they execute in the order of fastest to slowest
        [TestMethod]
        public void Load2ThenSaveIsDeterministic()
        {
            Debug.WriteLine("Unit test initialized for: Load2ThenSaveIsDeterministic()");
            foreach (FileInfo objBaseFileInfo in TestFiles)
            {
                // First Load-Save cycle
                string strDestinationControl = Path.Combine(TestPathInfo.FullName, "(Control) " + objBaseFileInfo.Name);
                using (Character objCharacter = LoadCharacter(objBaseFileInfo))
                    SaveCharacter(objCharacter, strDestinationControl);
                // Second Load-Save cycle
                string strDestinationTest = Path.Combine(TestPathInfo.FullName, "(Test) " + objBaseFileInfo.Name);
                using (Character objCharacter = LoadCharacter(new FileInfo(strDestinationControl)))
                    SaveCharacter(objCharacter, strDestinationTest);
                // Check to see that character after first load cycle is consistent with character after second
                using (FileStream controlFileStream = File.Open(strDestinationControl, FileMode.Open, FileAccess.Read))
                {
                    using (FileStream testFileStream = File.Open(strDestinationTest, FileMode.Open, FileAccess.Read))
                    {
                        try
                        {
                            Diff myDiff = DiffBuilder
                                .Compare(controlFileStream)
                                .WithTest(testFileStream)
                                .CheckForIdentical()
                                .WithNodeFilter(x => x.Name != "mugshot") // image loading and unloading is not going to be deterministic due to compression algorithms
                                .WithNodeMatcher(
                                    new DefaultNodeMatcher(
                                        ElementSelectors.Or(
                                            ElementSelectors.ByNameAndText,
                                            ElementSelectors.ByName)))
                                .IgnoreWhitespace()
                                .Build();
                            foreach (Difference diff in myDiff.Differences)
                            {
                                Console.WriteLine(diff.Comparison);
                                Console.WriteLine();
                            }

                            Assert.IsFalse(myDiff.HasDifferences(), myDiff.ToString());
                        }
                        catch (XmlSchemaException e)
                        {
                            Assert.Fail("Unexpected validation failure: " + e.Message);
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void Load3ThenPrint()
        {
            Debug.WriteLine("Unit test initialized for: Load3ThenExport()");
            string strLanguageDirectoryPath = Path.Combine(Utils.GetStartupPath, "lang");
            foreach(string strFilePath in Directory.GetFiles(strLanguageDirectoryPath, "*.xml"))
            { 
                string strExportLanguage = Path.GetFileNameWithoutExtension(strFilePath);
                if (strExportLanguage.Contains("data"))
                    continue;
                CultureInfo objExportCultureInfo = new CultureInfo(strExportLanguage);
                Debug.WriteLine("Unit test initialized for: Load3ThenExport() - Exporting for Culture " + objExportCultureInfo.Name);
                foreach (FileInfo objFileInfo in TestFiles)
                {
                    string strDestination = Path.Combine(TestPathInfo.FullName, strExportLanguage + ' ' + objFileInfo.Name);
                    using (Character objCharacter = LoadCharacter(objFileInfo))
                    {
                        XmlDocument xmlCharacter = new XmlDocument { XmlResolver = null };
                        // Write the Character information to a MemoryStream so we don't need to create any files.
                        MemoryStream objStream = new MemoryStream();
                        using (XmlTextWriter objWriter = new XmlTextWriter(objStream, Encoding.UTF8))
                        {
                            // Being the document.
                            objWriter.WriteStartDocument();

                            // </characters>
                            objWriter.WriteStartElement("characters");

#if DEBUG
                            objCharacter.PrintToStream(objStream, objWriter, objExportCultureInfo, strExportLanguage);
#else
                            objCharacter.PrintToStream(objWriter, objExportCultureInfo, strExportLanguage);
#endif

                            // </characters>
                            objWriter.WriteEndElement();

                            // Finish the document and flush the Writer and Stream.
                            objWriter.WriteEndDocument();
                            objWriter.Flush();

                            // Read the stream.
                            objStream.Position = 0;
                            using (StreamReader objReader = new StreamReader(objStream, Encoding.UTF8, true))
                                using (XmlReader objXmlReader = XmlReader.Create(objReader, GlobalOptions.SafeXmlReaderSettings))
                                    xmlCharacter.Load(objXmlReader);
                            xmlCharacter.Save(strDestination);
                        }
                    }
                }
            }
        }

        

        // Test methods have a number in their name so that by default they execute in the order of fastest to slowest
        [TestMethod]
        public void Load4CharacterForms()
        {
            Debug.WriteLine("Unit test initialized for: Load4CharacterForms()");
            frmChummerMain frmOldMainForm = Program.MainForm;
            using (frmChummerMain frmTestForm = new frmChummerMain(true)
            {
                WindowState = FormWindowState.Minimized,
                ShowInTaskbar = false // This lets the form be "shown" in unit tests (to actually have it show, ShowDialog() needs to be used)
            })
            {
                Program.MainForm = frmTestForm; // Set program Main form to Unit test version
                frmTestForm.Show(); // We don't actually want to display the main form, so Show() is used (ShowDialog() would actually display it).
                foreach (FileInfo objFileInfo in TestFiles)
                {
                    using (Character objCharacter = LoadCharacter(objFileInfo))
                    {
                        try
                        {
                            using (CharacterShared frmCharacterForm = objCharacter.Created
                                ? (CharacterShared) new frmCareer(objCharacter)
                                : new frmCreate(objCharacter))
                            {
                                frmCharacterForm.MdiParent = frmTestForm;
                                frmCharacterForm.WindowState = FormWindowState.Minimized;
                                frmCharacterForm.Show();
                            }
                        }
                        catch (Exception e)
                        {
                            string strErrorMessage = "Exception while loading form for " + objFileInfo.FullName + ":";
                            strErrorMessage += Environment.NewLine + e;
                            Debug.WriteLine(strErrorMessage);
                            Console.WriteLine(strErrorMessage);
                            Assert.Fail(strErrorMessage);
                        }
                    }
                }
            }
            Program.MainForm = frmOldMainForm;
        }

        /// <summary>
        /// Validate that a given list of Characters can be successfully loaded.
        /// </summary>
        private Character LoadCharacter(FileInfo objFileInfo)
        {
            Debug.WriteLine("Unit test initialized for: LoadCharacter()");
            Character objCharacter = null;
            try
            {
                Debug.WriteLine("Loading: " + objFileInfo.Name);
                objCharacter = new Character
                {
                    FileName = objFileInfo.FullName
                };
                Assert.IsTrue(objCharacter.Load().Result);
                Debug.WriteLine("Character loaded: " + objCharacter.Name);
            }
            catch (AssertFailedException e)
            {
                objCharacter?.Dispose();
                objCharacter = null;
                string strErrorMessage = "Could not load " + objFileInfo.FullName + "!";
                strErrorMessage += Environment.NewLine + e;
                Debug.WriteLine(strErrorMessage);
                Console.WriteLine(strErrorMessage);
                Assert.Fail(strErrorMessage);
            }
            catch (Exception e)
            {
                objCharacter?.Dispose();
                string strErrorMessage = "Exception while loading " + objFileInfo.FullName + ":";
                strErrorMessage += Environment.NewLine + e;
                Debug.WriteLine(strErrorMessage);
                Console.WriteLine(strErrorMessage);
                Assert.Fail(strErrorMessage);
            }

            return objCharacter;
        }

        /// <summary>
        /// Tests saving a given character.
        /// </summary>
        private void SaveCharacter(Character c, string path)
        {
            Debug.WriteLine("Unit test initialized for: SaveCharacter()");
            Assert.IsNotNull(c);
            try
            {
                c.Save(path, false);
            }
            catch (AssertFailedException e)
            {
                string strErrorMessage = "Could not load " + c.FileName + "!";
                strErrorMessage += Environment.NewLine + e;
                Debug.WriteLine(strErrorMessage);
                Console.WriteLine(strErrorMessage);
                Assert.Fail(strErrorMessage);
            }
            catch (InvalidOperationException e)
            {
                string strErrorMessage = "Could not save to " + path + "!";
                strErrorMessage += Environment.NewLine + e;
                Debug.WriteLine(strErrorMessage);
                Console.WriteLine(strErrorMessage);
                Assert.Fail(strErrorMessage);
            }
            catch (Exception e)
            {
                string strErrorMessage = "Exception while loading " + c.FileName + ":";
                strErrorMessage += Environment.NewLine + e;
                Debug.WriteLine(strErrorMessage);
                Console.WriteLine(strErrorMessage);
                Assert.Fail(strErrorMessage);
            }
        }
    }
}
