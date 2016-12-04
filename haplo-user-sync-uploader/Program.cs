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
            // SET HAPLO_SERVER=research.university.ac.uk
            // SET HAPLO_API_KEY=ABCD1234ABCD1234ABCD1234ABCD1234ABCD1234ABCD1234

            var hostname = Environment.GetEnvironmentVariable("HAPLO_SERVER");
            var API = Environment.GetEnvironmentVariable("HAPLO_API_KEY");

            // Check we have those initialised!
            // TODO: Return error codes as per https://msdn.microsoft.com/en-us/library/windows/desktop/ms681382(v=vs.85).aspx

            if ( String.IsNullOrEmpty(hostname) )
            {
                Console.WriteLine("HAPLO_SERVER environment variable has not been set.\n");
                return;
            }

            if ( String.IsNullOrEmpty(API) )
            {
                Console.WriteLine("HAPLO_API_KEY environment variable has not been set.\n");
                return;
            }

            // Copy the command line arguments array into a list for simple parsing.
            List<string> ArgList = new List<string>(args);

            // If we want verbose
            int Verbose = ArgList.IndexOf("--digest");

            if (Verbose >= 0)
            {
                // Chatty mode enabled.
                Console.WriteLine("HAPLO_SERVER={0}\nHAPLO_API_KEY={1}\n", hostname, API);

                // Remove the flag from the list, leaving the other parameters in the expected order. 
                ArgList.RemoveAt(Verbose);
            }

            // Check command line arguments. Typical invocation syntax would be ...
            //
            // haplo-user-sync-uploader file  students  path\to\ex_students.tsv
            // haplo-user-sync-uploader file  staff  path\to\ex_staff.tsv
            // haplo-user-sync-uploader apply

            string command;

            try
            {
                // Do we have a command as our first parameter?
                command = ArgList[0].ToLower();
            }
            catch
            {
                // No command provided - specify syntax, then exit.
                Console.WriteLine("Missing command\nSyntax: haplo-user-sync-uploader command [name] [filename]");
                return;
            }

            // We have a valid command - let's see whether we need a filename or not.
            string filename = "";
            string method = "";
            string name = "";

            // The default target 
            string target = "/api/haplo-user-sync/";

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
                        // Do we have a name & filename as our second & third parameters?
                        name = ArgList[1];
                        filename = ArgList[2];
                    }
                    catch
                    {
                        // Either no name provided, or missing filename - specify syntax, then exit.
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

                // Test command - n.
                case "test":
                {
                    // When testing, we simply fetch the root on the server and leave the method empty.
                    target = "/";
                    break;
                }


            }

            // Build target URI from hostname and method. Typical URI is ...
            // https:://dev5a36.infomanaged.co.uk/api/haplo-user-sync/upload-file 

            string uri = "https://" + hostname + target + method;

            // Our web client.
            WebClient webClient = new WebClient();

            // Populate headers.
            webClient.Headers.Add("User-Agent", "Haplo User Sync Uploader");

            // The auth header is of the form ...
            // Authorization: Basic ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+////=
            //
            // Where ABCD...////= is a Base64 encoded version of ...
            // haplo:APIKEYAPIKEYAPIKEYAPIKEYAPIKEY

            var bytes = Encoding.UTF8.GetBytes("haplo:"+ API);
           
            var auth = "Basic " + Convert.ToBase64String(bytes);

            // We only send the auth header if we have a method that requires it ...
            // i.e. if we're accessing the Haplo API - not if we're doing a test GET request.
            if (!String.IsNullOrEmpty(method))
            {
                webClient.Headers.Add("Authorization", auth);
            }

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

                    // Unknown or missing method - so we're doing a test.
                    default:
                    {
                        // Testing means we just do a GET request on the root - quick and easy way to check we're using the right server!
                        result = webClient.DownloadString(uri);
                        break;
                    }
                }
            }

            // Catch failing HTTP POSTs ...
            catch (WebException e)
            {
                // Output the exception's plain text message ...
                Console.WriteLine(e.Message);

                // Fetch any textual response from the server.
                // (The response may be null if the request didn't get that far - e.g. bad hostname.

                if ( null != e.Response )
                {
                    var reader = new StreamReader(e.Response.GetResponseStream());
                    result = reader.ReadToEnd();
                }

            }

            // Success is quiet by default, but chatty on request.
            if (Verbose >= 0)
            {
                // Let's see what we got.
                Console.WriteLine("{0}\n", result);
            }
        }
    }
}
