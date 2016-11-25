/* Haplo Platform                                     http://haplo.org
 * (c) Haplo Services Ltd 2006 - 2016    http://www.haplo-services.com
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.         */

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;   // For WebClient
using System.IO;    // For StreamReader



namespace haplo_user_sync_uploader
{
    class Program
    {
        static void Main(string[] args)
        {
            // Fetch Windows environment variables, assigned externally by ...
            // SET HAPLO_SERVER = research.university.ac.uk

            var hostname = Environment.GetEnvironmentVariable("HAPLO_SERVER");
            var API = Environment.GetEnvironmentVariable("HAPLO_API_KEY");

            // To do - check we have those initialised!

            // Typical invocation syntax would be ...
            //
            // haplo-user-sync-uploader file  students  path\to\ex_students.tsv
            // haplo-user-sync-uploader file  staff  path\to\ex_staff.tsv
            // haplo-user-sync-uploader apply

            string command;

            try
            {
                // Do we have a command as our first paramater?
                command = args[0].ToLower();
            }
            catch
            {
                // No command provided - specify syntax, then exit.
                Console.WriteLine("Missing command\nSyntax: haplo-user-sync-uploader command [name] [filename]");
                return;
            }

            // We have a valid command - let's see whether we need a filename or not.
            string filename = "";
            string method;
            string name = "";

            switch (command)
            {
                // Unknown or missing command - specify syntax, then exit.
                default:
                {
                    Console.WriteLine("Unknown command \"{0}\"\nSyntax: haplo-user-sync-uploader command [name] [filename]", command);
                    return;
                }

                // We're uploading a file.
                case "file":
                {
                    try
                    {
                        // Do we have a name & filename as our second & third paramaters?
                        name = args[1];         // NOT! Path.GetFileNameWithoutExtension(filename);
                        filename = args[2];
                    }
                    catch
                    {
                        // Either filename provided, or the file doesn't exist - specify syntax, then exit.
                        Console.WriteLine("Missing or invalid filename\nSyntax: haplo-user-sync-uploader file [name] [filename]");
                        return;
                    }

                    // Valid file to upload.
                    method = "upload-file";
                    break;
                }

                // We've been asked to start the synchronisation.
                case "apply":
                {
                    // All we need to do is set the method.
                    method = "start-sync";

                    break;
                }

            }

            // Build target URI from hostname and method. Typical URI is ...
            // https:://dev5a36.infomanaged.co.uk/api/haplo-user-sync/upload-file 

            string uri = "https://" + hostname + "/api/haplo-user-sync/" + method;

            // Our web client.
            WebClient webClient = new WebClient();

            // Populate headers.
            webClient.Headers.Add("User-Agent", "Haplo User Sync Uploader");

            // The auth header is of the form ...
            // Authorization: Basic ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+////=
            //
            // Where ABCD...////= is a Base64 encoded version of ...
            // haplo: APIKEYAPIKEYAPIKEYAPIKEYAPIKEY

            var bytes = Encoding.UTF8.GetBytes("haplo:"+ API);
           
            var auth = "Basic " + Convert.ToBase64String(bytes);

            webClient.Headers.Add("Authorization", auth);

            // So, having added all our headers and constructed the target URL,
            // we invoke the appropriate method for the command specified.

            string result = "";

            try
            {
                switch (method)
                {
                    // User has specified 'file' command.
                    case "upload-file":
                    {
                        webClient.QueryString.Add("name", name);
                        bytes = webClient.UploadFile(uri, filename);

                        // UploadFile returns a bytestream, but we know we'll always get a plaintext response.
                        result = System.Text.Encoding.Default.GetString(bytes);
                        break;
                    }

                    // User has specified 'apply' command.
                    case "start-sync":
                    {
                        result = webClient.UploadString(uri, "POST", "");
                        break;
                    }

                    // Unknown or missing method - this is a REALLY weird and unexpect internal error!
                    // Warn user, then exit.
                    default:
                    {
                        Console.WriteLine("Problem while attempting to perform method \"{0}\" \n", method);
                        return;
                    }
                }
            }

            // If any of the actual HTTP POSTs fail, we catch them here.
            catch (WebException e)
            {
                // Output the exception's plain text message ...
                Console.WriteLine(e.Message);

                // And fetch the textual response from the server.
                var reader = new StreamReader(e.Response.GetResponseStream());
                result = reader.ReadToEnd();
                
            }

            // Let's see what we got.
            // ToDo - make success quiet by default, but chatty on request.
            
            Console.WriteLine("{0}\n", result);

        }
    }
}
