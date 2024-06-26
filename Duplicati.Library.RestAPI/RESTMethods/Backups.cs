// Copyright (C) 2024, The Duplicati Team
// https://duplicati.com, hello@duplicati.com
// 
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"), 
// to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS 
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
using System;
using System.Linq;
using System.Collections.Generic;
using Duplicati.Server.Serialization;
using System.IO;
using Duplicati.Library.RestAPI;

namespace Duplicati.Server.WebServer.RESTMethods
{
    public class Backups : IRESTMethodGET, IRESTMethodPOST, IRESTMethodDocumented
    {
        public class AddOrUpdateBackupData
        {
            public Boolean IsUnencryptedOrPassphraseStored { get; set;}
            public Database.Schedule Schedule { get; set;}
            public Database.Backup Backup { get; set;}
        }

        public void GET(string key, RequestInfo info)
        {
            var schedules = FIXMEGlobal.DataConnection.Schedules;
            var backups = FIXMEGlobal.DataConnection.Backups;

            var all = from n in backups
                select new AddOrUpdateBackupData {
                IsUnencryptedOrPassphraseStored = FIXMEGlobal.DataConnection.IsUnencryptedOrPassphraseStored(long.Parse(n.ID)),
                Backup = (Database.Backup)n,
                Schedule = 
                    (from x in schedules
                        where x.Tags != null && x.Tags.Contains("ID=" + n.ID)
                        select (Database.Schedule)x).FirstOrDefault()
                };

            info.BodyWriter.OutputOK(all.ToArray());
        }

        private void ImportBackup(RequestInfo info)
        {
            var output_template = "<html><body><script type=\"text/javascript\">var jso = 'JSO'; var rp = null; try { rp = parent['CBM']; } catch (e) {}; if (rp) { rp('MSG', jso); } else { alert; rp('MSG'); };</script></body></html>";
            //output_template = "<html><body><script type=\"text/javascript\">alert('MSG');</script></body></html>";
            try
            {
                var input = info.Request.Form;
                var cmdline = Library.Utility.Utility.ParseBool(input["cmdline"].Value, false);
                var import_metadata = Library.Utility.Utility.ParseBool(input["import_metadata"].Value, false);
                var direct = Library.Utility.Utility.ParseBool(input["direct"].Value, false);
                output_template = output_template.Replace("CBM", input["callback"].Value);
                if (cmdline)
                {
                    info.Response.ContentType = "text/html";
                    info.BodyWriter.Write(output_template.Replace("MSG", "Import from commandline not yet implemented"));
                }
                else
                {
                    var file = info.Request.Form.GetFile("config");
                    if (file == null)
                        throw new Exception("No file uploaded");

                    Serializable.ImportExportStructure ipx = Backups.LoadConfiguration(file.Filename, import_metadata, () => input["passphrase"].Value);
                    if (direct)
                    {
                        lock (FIXMEGlobal.DataConnection.m_lock)
                        {
                            var basename = ipx.Backup.Name;
                            var c = 0;
                            while (c++ < 100 && FIXMEGlobal.DataConnection.Backups.Any(x => x.Name.Equals(ipx.Backup.Name, StringComparison.OrdinalIgnoreCase)))
                                ipx.Backup.Name = basename + " (" + c.ToString() + ")";

                            if (FIXMEGlobal.DataConnection.Backups.Any(x => x.Name.Equals(ipx.Backup.Name, StringComparison.OrdinalIgnoreCase)))
                            {
                                info.BodyWriter.SetOK();
                                info.Response.ContentType = "text/html";
                                info.BodyWriter.Write(output_template.Replace("MSG", "There already exists a backup with the name: " + basename.Replace("\'", "\\'")));
                            }

                            var err = FIXMEGlobal.DataConnection.ValidateBackup(ipx.Backup, ipx.Schedule);
                            if (!string.IsNullOrWhiteSpace(err))
                            {
                                info.ReportClientError(err, System.Net.HttpStatusCode.BadRequest);
                                return;
                            }

                            FIXMEGlobal.DataConnection.AddOrUpdateBackupAndSchedule(ipx.Backup, ipx.Schedule);
                        }

                        info.Response.ContentType = "text/html";
                        info.BodyWriter.Write(output_template.Replace("MSG", "OK"));

                    }
                    else
                    {
                        using (var sw = new StringWriter())
                        {
                            Serializer.SerializeJson(sw, ipx, true);
                            output_template = output_template.Replace("'JSO'", sw.ToString());
                        }
                        info.BodyWriter.Write(output_template.Replace("MSG", "Import completed, but a browser issue prevents loading the contents. Try using the direct import method instead."));
                    }
                }
            }
            catch (Exception ex)
            {
                FIXMEGlobal.DataConnection.LogError("", "Failed to import backup", ex);
                info.Response.ContentType = "text/html";
                info.BodyWriter.Write(output_template.Replace("MSG", ex.Message.Replace("\'", "\\'").Replace("\r", "\\r").Replace("\n", "\\n")));
            }            
        }

        public static Serializable.ImportExportStructure ImportBackup(string configurationFile, bool importMetadata, Func<string> getPassword, Dictionary<string, string> advancedOptions)
        {
            // This removes the ID and DBPath from the backup configuration.
            Serializable.ImportExportStructure importedStructure = Backups.LoadConfiguration(configurationFile, importMetadata, getPassword);

            // This will create the Duplicati-server.sqlite database file if it doesn't exist.
            using (Duplicati.Server.Database.Connection connection = FIXMEGlobal.GetDatabaseConnection(advancedOptions))
            {
                if (connection.Backups.Any(x => x.Name.Equals(importedStructure.Backup.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"A backup with the name {importedStructure.Backup.Name} already exists.");
                }

                string error = connection.ValidateBackup(importedStructure.Backup, importedStructure.Schedule);
                if (!string.IsNullOrWhiteSpace(error))
                {
                    throw new InvalidOperationException(error);
                }

                // This creates a new ID and DBPath.
                connection.AddOrUpdateBackupAndSchedule(importedStructure.Backup, importedStructure.Schedule);
            }

            return importedStructure;
        }

        private static Serializable.ImportExportStructure LoadConfiguration(string filename, bool importMetadata, Func<string> getPassword)
        {
            Serializable.ImportExportStructure ipx;

            var buf = new byte[3];
            using (var fs = System.IO.File.OpenRead(filename))
            {
                Duplicati.Library.Utility.Utility.ForceStreamRead(fs, buf, buf.Length);

                fs.Position = 0;
                if (buf[0] == 'A' && buf[1] == 'E' && buf[2] == 'S')
                {
                    using (var m = new Duplicati.Library.Encryption.AESEncryption(getPassword(), new Dictionary<string, string>()))
                    {
                        using (var m2 = m.Decrypt(fs))
                        {
                            using (var sr = new System.IO.StreamReader(m2))
                            {
                                ipx = Serializer.Deserialize<Serializable.ImportExportStructure>(sr);
                            }
                        }
                    }
                }
                else
                {
                    using (var sr = new System.IO.StreamReader(fs))
                    {
                        ipx = Serializer.Deserialize<Serializable.ImportExportStructure>(sr);
                    }
                }
            }

            if (ipx.Backup == null)
            {
                throw new Exception("No backup found in document");
            }

            if (ipx.Backup.Metadata == null)
            {
                ipx.Backup.Metadata = new Dictionary<string, string>();
            }

            if (!importMetadata)
            {
                ipx.Backup.Metadata.Clear();
            }

            ipx.Backup.ID = null;
            ipx.Backup.DBPath = null;

            if (ipx.Schedule != null)
            {
                ipx.Schedule.ID = -1;
            }

            return ipx;
        }

        public void POST(string key, RequestInfo info)
        {
            if ("import".Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                ImportBackup(info);
                return;
            }

            AddOrUpdateBackupData data = null;
            try
            {
                var str = info.Request.Form["data"].Value;
                if (string.IsNullOrWhiteSpace(str)) {
                    str = System.Threading.Tasks.Task.Run(async () =>
                    {
                        using (var sr = new System.IO.StreamReader(info.Request.Body, System.Text.Encoding.UTF8, true))

                            return await sr.ReadToEndAsync();
                    }).GetAwaiter().GetResult();
                }

                data = Serializer.Deserialize<AddOrUpdateBackupData>(new StringReader(str));
                if (data.Backup == null)
                {
                    info.ReportClientError("Data object had no backup entry", System.Net.HttpStatusCode.BadRequest);
                    return;
                }

                data.Backup.ID = null;

                if (Duplicati.Library.Utility.Utility.ParseBool(info.Request.Form["temporary"].Value, false))
                {
                    using(var tf = new Duplicati.Library.Utility.TempFile())
                        data.Backup.DBPath = tf;

                    data.Backup.Filters = data.Backup.Filters ?? new Duplicati.Server.Serialization.Interface.IFilter[0];
                    data.Backup.Settings = data.Backup.Settings ?? new Duplicati.Server.Serialization.Interface.ISetting[0];

                    FIXMEGlobal.DataConnection.RegisterTemporaryBackup(data.Backup);

                    info.OutputOK(new { status = "OK", ID = data.Backup.ID });
                }
                else
                {
                    if (Library.Utility.Utility.ParseBool(info.Request.Form["existing_db"].Value, false))
                    {
                        data.Backup.DBPath = Library.Main.DatabaseLocator.GetDatabasePath(data.Backup.TargetURL, null, false, false);
                        if (string.IsNullOrWhiteSpace(data.Backup.DBPath))
                            throw new Exception("Unable to find remote db path?");
                    }


                    lock(FIXMEGlobal.DataConnection.m_lock)
                    {
                        if (FIXMEGlobal.DataConnection.Backups.Any(x => x.Name.Equals(data.Backup.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            info.ReportClientError("There already exists a backup with the name: " + data.Backup.Name, System.Net.HttpStatusCode.Conflict);
                            return;
                        }

                        var err = FIXMEGlobal.DataConnection.ValidateBackup(data.Backup, data.Schedule);
                        if (!string.IsNullOrWhiteSpace(err))
                        {
                            info.ReportClientError(err, System.Net.HttpStatusCode.BadRequest);
                            return;
                        }

                        FIXMEGlobal.DataConnection.AddOrUpdateBackupAndSchedule(data.Backup, data.Schedule);
                    }

                    info.OutputOK(new { status = "OK", ID = data.Backup.ID });
                }
            }
            catch (Exception ex)
            {
                if (data == null)
                    info.ReportClientError(string.Format("Unable to parse backup or schedule object: {0}", ex.Message), System.Net.HttpStatusCode.BadRequest);
                else
                    info.ReportClientError(string.Format("Unable to save schedule or backup object: {0}", ex.Message), System.Net.HttpStatusCode.InternalServerError);
            }
        }


        public string Description { get { return "Return a list of current backups and their schedules"; } }

        public IEnumerable<KeyValuePair<string, Type>> Types
        {
            get
            {
                return new KeyValuePair<string, Type>[] {
                    new KeyValuePair<string, Type>(HttpServer.Method.Get, typeof(AddOrUpdateBackupData[])),
                    new KeyValuePair<string, Type>(HttpServer.Method.Post, typeof(AddOrUpdateBackupData))
                };
            }
        }
    }
}

