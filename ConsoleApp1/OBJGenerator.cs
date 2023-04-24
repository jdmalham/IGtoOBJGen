﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using IGtoOBJGen;
/*

What's going on in this file? It's just the main file. Why is it so messy? Good question. 

 */
class OBJGenerator
{
     static void Main(string[] args)
    {
        Unzip zipper = new Unzip(@"C:\Users\uclav\Desktop\IG\Hto4l_120-130GeV.ig");
        StreamReader file;
        string eventName;
        string strPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var watch = new Stopwatch();
        watch.Start();

        
        bool inputState = args.Length == 0;
        if (inputState == true){
            file = File.OpenText(@"C:\Users\uclav\Source\Repos\jdmalham\IG-File-OBJ-Generator\ConsoleApp1\IGdata\Event_1096322990");
            eventName = "Event_1096322990";
        } else {
            /*  Right so what's all this? We get the name of the event and then
            find and replace all occurrences of nan that are in the original file
            with null so that the JSON library can properly parse it. Store the revisions in a temp file that
            is deleted at the end of the program's execution so that the original file goes unchanged and can 
            still be used with iSpy  */
            string destination = zipper.currentFile;
            string[] split = destination.Split('\\');
            eventName = split.Last();
            Console.WriteLine(eventName);
            
            string text = File.ReadAllText($"{destination}");
            string newText = text.Replace("nan,","null,");
            
            File.WriteAllText($"{args[0]}.tmp",newText );
            file = File.OpenText($"{args[0]}.tmp");
        }
 
        JsonTextReader reader = new JsonTextReader(file);
        JObject o2 = (JObject)JToken.ReadFrom(reader);

        file.Close();

        if (inputState == false)
        {
            File.Delete($"{args[0]}.tmp");
        }

        IGTracks trackHandler = new IGTracks(o2, eventName);
        IGBoxes boxHandler = new IGBoxes(o2,eventName);

        
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!


        /*
        This code block is QUARANTINED!!! This code must be not touched until the class can be rewritten to achieve the same result in its constructor.
        Currently, this is a very bodged way of ensuring that all calorimetry objects get parsed and not very conducive to stability.
        Needs refactoring not only to ensure stability, but also make the main code file much easier to read and understand.
        Passing a variable into the function that will be defining it almost like some weird recursion is not the move.
        */

        string[] calorimetryItems = { "EBRecHits_V2", "EERecHits_V2", "ESRecHits_V2", "HBRecHits_V2" };
        
        List<List<CalorimetryData>> boxObjectsGesamt = new List<List<CalorimetryData>>(); 

        foreach (string name in calorimetryItems)
        {
            boxObjectsGesamt = boxHandler.calorimetryParse( name, boxObjectsGesamt);
        }
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!




        foreach (var thing in boxObjectsGesamt)
        {
            if (thing.Count() == 0) continue;
            string name = thing[0].name;
            if (name == "HBRecHits_V2")
            {
                var contents = boxHandler.generateCalorimetryBoxes(thing);
                try
                {
                    File.WriteAllText($"{strPath}\\{eventName}\\{name}.obj", String.Empty);
                }
                catch (DirectoryNotFoundException)
                {
                    Directory.CreateDirectory($"{strPath}\\{eventName}");
                    File.WriteAllText($"{strPath}\\{eventName}\\{name}.obj", String.Empty);
                }
                File.WriteAllLines($"{strPath}\\{eventName}\\{name}.obj", contents);
                continue;
            }
            
            List<string> Contents = boxHandler.generateCalorimetryTowers(thing);
            try
            {
                File.WriteAllText($"{strPath}\\{eventName}\\{name}.obj", String.Empty);
            } 
            catch (DirectoryNotFoundException) 
            {
                Directory.CreateDirectory($"{strPath}\\{eventName}");
                File.WriteAllText($"{strPath}\\{eventName}\\{name}.obj", String.Empty);
            }
            File.WriteAllLines($"{strPath}\\{eventName}\\{name}.obj",Contents);

        }

        List<MuonChamberData> list = boxHandler.muonChamberParse(); 
        boxHandler.generateMuonChamberModels(list); 
        
        
        List<JetData> jetList = boxHandler.jetParse();
        boxHandler.generateJetModels(jetList);

        zipper.destroyStorage();

        /*try
        {
            Communicate bridge = new Communicate(@"C:\Users\uclav\AppData\Local\Android\Sdk\platform-tools\adb.exe");
            bridge.DownloadFiles("Photons_V1.obj");
        } catch (Exception e) {

            if (e is System.ArgumentOutOfRangeException)
            {
                Console.WriteLine("System.ArgumentOutOfRangeException thrown while trying to locate ADB.\nPlease check that ADB is installed and the proper path has been provided. The default path for Windows is C:\\Users\\[user]\\AppData\\Local\\Android\\sdk\\platform-tools\n");
            }
            else if (e is SharpAdbClient.Exceptions.AdbException)
            {
                Console.WriteLine("An ADB exception has been thrown.\nPlease check that the Oculus is connected to the computer.");
            }
            Environment.Exit(1);

        }*/
        //bridge.UploadFiles(trackHandler.filePaths);
        Console.WriteLine($"Total Execution Time: {watch.ElapsedMilliseconds} ms"); // See how fast code runs. Code goes brrrrrrr on fancy office pc. It makes me happy. :)
    }
}