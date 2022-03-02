using ForgeMdTest;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace forgemdTest
{
    internal  class ConfigOptions
    {
        
        public static string? InputPath { get; set; }
        public static string? StorageRegion { get; set; }
        public static string? ServerEndpoint { get; set; }
        public static string? DerivativeRegion { get; set; }

        public static string? Outputs { get; set; }
    }
    internal class CommandBuilder
    {
        public RootCommand RootCommand { get; set; }
        public ConfigOptions? ConfigOptions { get; set; }
        public CommandBuilder()
        {
            RootCommand = new RootCommand("An utility to automate forge model derivative operations service")
            {
                Name = "forgemd"
            };           
        }
        public RootCommand Build()
        {
            AddRun();
            AddClean();
            return RootCommand;
        }
        public void AddRun()
        {
            var runner = new Command("run", "translates input source to obj file");
            var path = new Argument<string>("input", "input path of source file");
            var se = new Option<string>(new[] { "--server", "-s" }, "Derivative Service Endpoint US or EU")
            {
                IsRequired = false
            };
            se.SetDefaultValue("us");
            var sr = new Option<string>(new[] { "--region", "-r" }, "File Storage region US or EU")
            {
                IsRequired = false
            };
            sr.SetDefaultValue("us");
            var tr = new Option<string>(new[] { "--target", "-t" }, "Target storage region for resultant derivatives ")
            {
                IsRequired = false
            };
            tr.SetDefaultValue("us");
            var output = new Option<string>(new[] { "--output", "-o" }, "output local directory to download obj files ")
            {
                IsRequired = false
            };
            output.SetDefaultValue(Environment.CurrentDirectory);
            runner.AddArgument(path);
            runner.AddOption(se);
            runner.AddOption(sr);
            runner.AddOption(tr);
            runner.AddOption(output);
            runner.SetHandler( async (string path, string se, string sr, string tr, string op) =>
            {
                ConfigOptions.InputPath = path;
                ConfigOptions.Outputs = op;
                ConfigOptions.ServerEndpoint =se;
                ConfigOptions.StorageRegion = sr;
                ConfigOptions.DerivativeRegion = tr;
                await Program.RunWorkFlow();                
            },path,se,sr,tr,output);
            RootCommand.AddCommand(runner);


        }

        public void AddClean()
        {
            var cleaner = new Command("clean", "clean's up derivative manifest and oss-bucket as well");
            var urn = new Option<string>(new[] { "--urn", "-u" }, "Derivative URN you would like to clean up")
            {
                IsRequired = true
            };
            cleaner.AddOption(urn);
            var se = new Option<string>(new[] { "--server", "-s" }, "Derivative Service Endpoint US or EU")
            {
                IsRequired = false
            };
            se.SetDefaultValue("us");
            cleaner.AddOption(se);           
            cleaner.SetHandler(async (string urn, string se) =>
            {
                try
                {

                    Region endpoint = Enum.Parse<Region>(se, true);                    
                    await Program.Clean(urn, endpoint);
                }
                catch (Exception) { }
            },urn,se);
            RootCommand.AddCommand(cleaner);            
        }

    }
}
