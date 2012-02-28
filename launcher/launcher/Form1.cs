using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Net;

using System.IO;

namespace launcher
{
    public partial class Form1 : Form
    {
        StreamWriter w;

        public Form1()
        {
            InitializeComponent();
            w = File.AppendText("log.txt");
            w.WriteLine("======================================");
            w.WriteLine("Starting log for run of app on " + DateTime.Now);
            w.Flush();
            if(File.Exists("ineedtodosomesigning.plz")) button1.Visible=true;
        }

        private void label1_Click(object sender, EventArgs e)
        {
            
        }

        public void Log(String label, String text)
        {
            w.WriteLine(DateTime.Now+": "+label+"- "+text);
            w.Flush();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Log("Sign", "Starting xml sign");

            try
            {
                // Create a new CspParameters object to specify
                // a key container.
                CspParameters cspParams = new CspParameters();
                cspParams.KeyContainerName = "XML_DSIG_RSA_KEY";

                // Create a new RSA signing key and save it in the container. 
                RSACryptoServiceProvider rsaKey = new RSACryptoServiceProvider(cspParams);

                // Create a new XML document.
                XmlDocument xmlDoc = new XmlDocument();

                // Load an XML file into the XmlDocument object.
                xmlDoc.PreserveWhitespace = true;
                xmlDoc.Load("test.xml");

                // Sign the XML document. 
                SignXml(xmlDoc, rsaKey);

                Console.WriteLine("XML file signed.");

                // Save the document.
                xmlDoc.Save("test.xml");
                Log("Sign", "xml sign complete");
            }
            catch (Exception e2)
            {
                Log("ERROR Sign", e2.Message);
                Console.WriteLine(e2.Message);

            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Log("update XML check", "Downloading update xml");
            File.Delete("test.xml");
            WebClient Client = new WebClient();

           // Client.DownloadFile("http://www-personal.umich.edu/~amwinslo/test.xml", @"test.xml");
            Client.DownloadFile("http://secure.awins.info/test.xml", @"test.xml");
            Log("update XML check", "Downloaded, checking signiture");
            try
            {
                // Create a new CspParameters object to specify
                // a key container.
                CspParameters cspParams = new CspParameters();
                cspParams.KeyContainerName = "XML_DSIG_RSA_KEY";

                // Create a new RSA signing key and save it in the container. 
                RSACryptoServiceProvider rsaKey = new RSACryptoServiceProvider(cspParams);
                FileStream fileStream = new FileStream(@"file.txt", FileMode.Open);
                try
                {
                    long howMany = fileStream.Length;
                    byte[] publicKey = new byte[(int)howMany];
                    fileStream.Read(publicKey,0,(int)howMany);
                    
                    //fileStream.Write(rsaKey.ExportCspBlob(false), 0, rsaKey.ExportCspBlob(false).Length);
                    rsaKey.ImportCspBlob(publicKey);
                }
                finally
                {
                    fileStream.Close();
                }
                
                // Create a new XML document.
                XmlDocument xmlDoc2 = new XmlDocument();

                // Load an XML file into the XmlDocument object.
                xmlDoc2.PreserveWhitespace = true;
                xmlDoc2.Load("test.xml");

                // Verify the signature of the signed XML.
                Console.WriteLine("Verifying signature...");
                bool result = VerifyXml(xmlDoc2, rsaKey);

                // Display the results of the signature verification to 
                // the console.
                if (result)
                {
                    label1.Text = "The XML signature is valid.";
                    Log("update XML check", "Downloading update xml");
                    //label1.Text = rsaKey.ExportCspBlob(true).ToString();
                }
                else
                {
                    label1.Text = "The XML signature is not valid.";
                    Log("update XML check", "Downloading update xml");
                }


            }
            catch (Exception e2)
            {
                label1.Text = e2.Message;
                Console.WriteLine(e2.Message);

            }
        }


        // Sign an XML file. 
        // This document cannot be verified unless the verifying 
        // code has the key with which it was signed.
        public static void SignXml(XmlDocument Doc, RSA Key)
        {
            // Check arguments.
            if (Doc == null)
                throw new ArgumentException("Doc");
            if (Key == null)
                throw new ArgumentException("Key");

            // Create a SignedXml object.
            SignedXml signedXml = new SignedXml(Doc);

            // Add the key to the SignedXml document.
            signedXml.SigningKey = Key;

            // Create a reference to be signed.
            Reference reference = new Reference();
            reference.Uri = "";

            // Add an enveloped transformation to the reference.
            XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(env);

            // Add the reference to the SignedXml object.
            signedXml.AddReference(reference);

            // Compute the signature.
            signedXml.ComputeSignature();

            // Get the XML representation of the signature and save
            // it to an XmlElement object.
            XmlElement xmlDigitalSignature = signedXml.GetXml();

            // Append the element to the XML document.
            Doc.DocumentElement.AppendChild(Doc.ImportNode(xmlDigitalSignature, true));

        }

        // Verify the signature of an XML file against an asymmetric 
        // algorithm and return the result.
        public static Boolean VerifyXml(XmlDocument Doc, RSA Key)
        {
            // Check arguments.
            if (Doc == null)
                throw new ArgumentException("Doc");
            if (Key == null)
                throw new ArgumentException("Key");

            // Create a new SignedXml object and pass it
            // the XML document class.
            SignedXml signedXml = new SignedXml(Doc);

            // Find the "Signature" node and create a new
            // XmlNodeList object.
            XmlNodeList nodeList = Doc.GetElementsByTagName("Signature");

            // Throw an exception if no signature was found.
            if (nodeList.Count <= 0)
            {
                throw new CryptographicException("Verification failed: No Signature was found in the document.");
            }

            // This example only supports one signature for
            // the entire XML document.  Throw an exception 
            // if more than one signature was found.
            if (nodeList.Count >= 2)
            {
                throw new CryptographicException("Verification failed: More that one signature was found for the document.");
            }

            // Load the first <signature> node.  
            signedXml.LoadXml((XmlElement)nodeList[0]);

            // Check the signature and return the result.
            return signedXml.CheckSignature(Key);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            XmlDocument xmlDoc = new XmlDocument();

            // Load an XML file into the XmlDocument object.
            xmlDoc.PreserveWhitespace = true;
            xmlDoc.Load("test.xml");

            XmlNodeList nodeList = xmlDoc.GetElementsByTagName("file");


            for (int a = 0; a < nodeList.Count; a++)
            {
                XmlNode node = nodeList.Item(a);
                XmlAttributeCollection attr = node.Attributes;
                String name = attr.Item(0).Value;
                String address = attr.Item(1).Value;
                String hash = attr.Item(2).Value;

                if (File.Exists(name))
                {
                    FileStream f = new FileStream(name, FileMode.Open);
                    string fileHash = BitConverter.ToString((new SHA1CryptoServiceProvider()).ComputeHash(f));
                    fileHash = fileHash.ToLower();
                    fileHash = fileHash.Replace("-",string.Empty);
                    textBox1.Text = fileHash;
                    f.Close();
                    if (hash.Equals(fileHash)) label1.Text = "File Hash Match!";
                    else
                    {
                        label1.Text = "Hash Mismatch!, file will be replaced";
                        File.Delete(name);
                        WebClient Client = new WebClient();

                        Client.DownloadFile(address, name);
                    }
                }
                else
                {
                    label1.Text = "file doesnt exist, downloading...";
                    WebClient Client = new WebClient();

                    Client.DownloadFile(address, name);
                }
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
    
        private void Form1_Shown(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            Log("Update Check", "Starting check for updates...");
            label3.Visible = true;
            label1.Text = "Checking for updates...";
            button4.Enabled = false;
            Log("update XML check", "Downloading update xml and public key");
            if (File.Exists("test.xml")) File.Delete("test.xml");
            if (File.Exists("file.txt")) File.Delete("file.txt");
            WebClient Client = new WebClient();

            Client.DownloadFile("http://secure.awins.info/test.xml", @"test.xml");
            Client.DownloadFile("http://secure.awins.info/public.txt", @"file.txt");
            Log("update XML check", "Checing update XML's signature");
            try
            {
                // Create a new CspParameters object to specify
                // a key container.
                CspParameters cspParams = new CspParameters();
                cspParams.KeyContainerName = "XML_DSIG_RSA_KEY";

                // Create a new RSA signing key and save it in the container. 
                RSACryptoServiceProvider rsaKey = new RSACryptoServiceProvider(cspParams);
                FileStream fileStream = new FileStream(@"file.txt", FileMode.Open);
                try
                {
                    long howMany = fileStream.Length;
                    byte[] publicKey = new byte[(int)howMany];
                    fileStream.Read(publicKey, 0, (int)howMany);

                    //fileStream.Write(rsaKey.ExportCspBlob(false), 0, rsaKey.ExportCspBlob(false).Length);
                    rsaKey.ImportCspBlob(publicKey);
                }
                finally
                {
                    fileStream.Close();
                }

                // Create a new XML document.
                XmlDocument xmlDoc2 = new XmlDocument();

                // Load an XML file into the XmlDocument object.
                xmlDoc2.PreserveWhitespace = true;
                xmlDoc2.Load("test.xml");

                // Verify the signature of the signed XML.
                Console.WriteLine("Verifying signature...");
                bool result = VerifyXml(xmlDoc2, rsaKey);

                // Display the results of the signature verification to 
                // the console.
                if (result)
                {
                    File.Delete("file.txt");
                    Log("update XML check", "The XML signature is valid");
                    label1.Text = "The XML signature is valid.";
                    progressBar1.Value = 10;
                }
                else
                {
                    File.Delete("file.txt");
                    Log("ERROR update XML check", "The XML signature is not valid, closing...");
                    label1.Text = "The XML signature is not valid.";
                    MessageBox.Show("The update XML's signature is invalid, The launcher will now exit", "Error", MessageBoxButtons.OK);
                    this.Close();

                }


            }
            catch (Exception e2)
            {
                label1.Text = e2.Message;
                Log("ERROR update XML check", e2.Message);
                Console.WriteLine(e2.Message);

            }
            Log("update XML check", "Checking file hashes");

                        XmlDocument xmlDoc = new XmlDocument();

                        // Load an XML file into the XmlDocument object.
                        xmlDoc.PreserveWhitespace = true;
                        xmlDoc.Load("test.xml");
                        bool change = false;
                        XmlNodeList nodeList = xmlDoc.GetElementsByTagName("file");

                        XmlNodeList folderList = xmlDoc.GetElementsByTagName("folder");

                        for (int a = 0; a < folderList.Count; a++)
                        {
                            XmlNode node = folderList.Item(a);
                            if (!Directory.Exists(node.Attributes.Item(0).Value)) Directory.CreateDirectory(node.Attributes.Item(0).Value);
                        }

                        Log("file update check", nodeList.Count.ToString()+" files to check on");
                        int step = 90 / nodeList.Count;
                        for (int a = 0; a < nodeList.Count; a++)
                        {
                            XmlNode node = nodeList.Item(a);
                            XmlAttributeCollection attr = node.Attributes;
                            String name = attr.Item(0).Value;
                            String address = attr.Item(1).Value;
                            String hash = attr.Item(2).Value;
                            Log("file update check", "Checking: "+ name);
                            if (File.Exists(name))
                            {
                                Log("file update check", name + " exists");
                                FileStream f = new FileStream(name, FileMode.Open);
                                string fileHash = BitConverter.ToString((new SHA1CryptoServiceProvider()).ComputeHash(f));
                                fileHash = fileHash.ToLower();
                                fileHash = fileHash.Replace("-", string.Empty);
                                textBox1.Text = fileHash;
                                f.Close();
                                if (hash.Equals(fileHash))
                                {
                                    label1.Text = "File Hash Match!";
                                    Log("file update check", "The update hash and local hashes match for " + name);
                                    progressBar1.Value += step;
                                }
                                else
                                {
                                    label1.Text = "Hash Mismatch!, file will be replaced";
                                    Log("file update check", "The update hash and local hashes do not match for " + name + ", the file will be replaced");
                                    File.Delete(name);
                                   // WebClient Client = new WebClient();
                                    Uri hi = new Uri(address);
                                    Client.DownloadFile(hi, name);
                                    progressBar1.Value += step;
                                    change = true;
                                    label3.Visible = true;
                                }
                            }
                            else
                            {
                                Log("file update check", name + " does not exist, downloading...");
                                label1.Text = "file doesnt exist, downloading...";
                              //  WebClient Client = new WebClient();
                                Uri hi = new Uri(address);
                                Client.DownloadFile(hi, name);
                                progressBar1.Value += step;
                                change = true;
                                label3.Visible = true;
                            }
                        }
                        progressBar1.Value = 100;
                        if (!change) button4.Enabled = true;
                        else
                        {
                            Log("file update check", "All files have been updated, launcher is now restarting for integrity check");
                            MessageBox.Show("Your app has been updated. The launcher will now reload", "Update Complete", MessageBoxButtons.OK);
                            Application.Restart();
                        }


                        


                        if (!change)
                        {
                            Log("overall", "all quiet on the western front, launching the exe now");
                            System.Diagnostics.Process.Start(@"C:\Windows\System32\notepad.exe");
                            Application.Exit();
                        }
        }

    }
}
