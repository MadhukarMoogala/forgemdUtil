using Autodesk.Forge;
using Autodesk.Forge.Client;
using Autodesk.Forge.Model;
using forgemdTest;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;

namespace ForgeMdTest
{
    public enum Region
    {
        US,
        EMEA
    };


    internal class Program
    {
        #region Enums & Consts


        #endregion
        #region Fields & Properties
        private static string? FORGE_CLIENT_ID = Environment.GetEnvironmentVariable("FORGE_CLIENT_ID");
        private static string? FORGE_CLIENT_SECRET = Environment.GetEnvironmentVariable("FORGE_CLIENT_SECRET");
        private static string? FORGE_API_PATH = Environment.GetEnvironmentVariable("FORGE_API_PATH");


        private static readonly Scope[] SCOPES = new Scope[] {
            Scope.DataRead, Scope.DataWrite, Scope.DataCreate, Scope.DataSearch,
            Scope.BucketCreate, Scope.BucketRead, Scope.BucketUpdate, Scope.BucketDelete
        };
        protected static string? AccessToken { get; private set; }
        protected static string BucketKey { get { return ("forge_sample_" + FORGE_CLIENT_ID?.ToLower() + "-" + DerivativeLocation.ToString().ToLower()); } }
        protected static string? ObjectKey { get; set; }

        protected static Region DerivativeLocation { get; set; } = Region.US;


        protected static BucketsApi BucketAPI = new(FORGE_API_PATH);
        protected static ObjectsApi ObjectsAPI = new(FORGE_API_PATH);
        protected static DerivativesApi USDerivativesAPI = new(FORGE_API_PATH, false);
        protected static DerivativesApi EMEADerivativesAPI = new(FORGE_API_PATH, true);
        protected static DerivativesApi DerivativesAPI = USDerivativesAPI; // defaults to US
        protected static DerivativesApi DerivativesAPIAutoRegion { get { return (DerivativeLocation == Region.US ? USDerivativesAPI : EMEADerivativesAPI); } }

        #endregion

        #region Forge
        private async static Task<ApiResponse<dynamic>?> OauthExecAsync()
        {
            try
            {
                AccessToken = "";
                TwoLeggedApi _twoLeggedApi = new(FORGE_API_PATH);
                ApiResponse<dynamic> bearer = await _twoLeggedApi.AuthenticateAsyncWithHttpInfo(
                    FORGE_CLIENT_ID, FORGE_CLIENT_SECRET, oAuthConstants.CLIENT_CREDENTIALS, SCOPES);
                HttpErrorHandler(bearer, "Failed to get your token");

                AccessToken = bearer.Data.access_token;
                BucketAPI.Configuration.AccessToken = AccessToken;
                ObjectsAPI.Configuration.AccessToken = AccessToken;
                USDerivativesAPI.Configuration.AccessToken = AccessToken;
                EMEADerivativesAPI.Configuration.AccessToken = AccessToken;

                return (bearer);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception when calling TwoLeggedApi.AuthenticateAsyncWithHttpInfo : " + ex.Message);
                return null;
            }
        }

        private static dynamic? GetBucketDetails()
        {
            try
            {
                Console.WriteLine("**** Getting bucket details for: " + BucketKey);
                dynamic response = BucketAPI.GetBucketDetails(BucketKey);
                return (response);
            }
            catch (Exception)
            {
                Console.WriteLine("**** Failed getting bucket details for : " + BucketKey);
                return null; ;
            }
        }

        private static dynamic? CreateBucket()
        {
            try
            {
                Console.WriteLine("**** Creating bucket: " + BucketKey);
                PostBucketsPayload.PolicyKeyEnum bucketType = PostBucketsPayload.PolicyKeyEnum.Persistent;
                PostBucketsPayload payload = new PostBucketsPayload(BucketKey, null, bucketType);
                dynamic response = BucketAPI.CreateBucketAsyncWithHttpInfo(payload, DerivativeLocation.ToString());
                return (response);
            }
            catch (Exception)
            {
                Console.WriteLine("**** Failed creating bucket: " + BucketKey);
                return null; ;
            }
        }

        private static bool CreateBucketIfNotExist()
        {
            dynamic? response = GetBucketDetails();
            if (response == null)
                response = CreateBucket();
            if (response == null)
                Console.WriteLine("*** Failed to create bucket: " + BucketKey);
            return (response != null);
        }

        private static bool DeleteBucket()
        {
            try
            {
                Console.WriteLine("**** Deleting bucket: " + BucketKey);
                BucketAPI.DeleteBucket(BucketKey);
                return (true);
            }
            catch (Exception)
            {
                Console.WriteLine("**** Failed deleting bucket: " + BucketKey);
                return (false);
            }
        }

        private static dynamic? GetObjectDetails()
        {
            try
            {
                Console.WriteLine("**** Getting object details: " + ObjectKey);
                dynamic response = ObjectsAPI.GetObjectDetails(BucketKey, ObjectKey);
                return (response);
            }
            catch (Exception)
            {
                Console.WriteLine("**** Failed getting object details: " + ObjectKey);
                return null; ;
            }
        }

        private static string? UploadObject2Bucket(string input)
        {
            try
            {
                Console.WriteLine("**** Uploading object: " + ObjectKey);
                using StreamReader streamReader = new(path: input);
                dynamic response = ObjectsAPI.UploadObjectWithHttpInfo(
                    BucketKey,
                    ObjectKey,
                    (int)streamReader.BaseStream.Length,
                    streamReader.BaseStream,
                    "application/octet-stream"
                );
                HttpErrorHandler(response, "Failed to upload file");
                return (response.Data["objectId"]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("**** Failed to upload file - " + ex.Message);
                return null;
            }
        }

        private static string? UploadSampleFile(string input)
        {
            dynamic? response = GetObjectDetails();
            if (response != null)
                return (response.objectId);
            response = UploadObject2Bucket(input);
            if (response == null)
            {
                Console.WriteLine("*** Failed to upload sample file: " + ObjectKey);
                return null;
            }
            return (response);
        }

        private static bool DeleteSampleFile()
        {
            try
            {
                Console.WriteLine("**** Deleting object: " + ObjectKey);
                ObjectsAPI.DeleteObject(BucketKey, ObjectKey);
                return (true);
            }
            catch (Exception)
            {
                Console.WriteLine("**** Failed deleting object: " + ObjectKey);
                return (false);
            }
        }

        private async static Task<bool> Translate2Svf(string urn, JobPayloadDestination.RegionEnum targetRegion = JobPayloadDestination.RegionEnum.US)
        {
            try
            {
                Console.WriteLine("**** Requesting SVF translation for: " + urn);
                JobPayloadInput jobInput = new JobPayloadInput(urn);
                JobPayloadOutput jobOutput = new JobPayloadOutput(
                    new List<JobPayloadItem>(
                        new JobPayloadItem[] {
                                new JobPayloadItem (
                                    JobPayloadItem.TypeEnum.Svf,
                                    new List<JobPayloadItem.ViewsEnum> (
                                        new JobPayloadItem.ViewsEnum [] {
                                            JobPayloadItem.ViewsEnum._2d, JobPayloadItem.ViewsEnum._3d
                                        }
                                    ),
                                    null
                                )
                        }
                    ),
                    new JobPayloadDestination(targetRegion)
                );
                JobPayload job = new JobPayload(jobInput, jobOutput);
                bool bForce = true;
                ApiResponse<dynamic> response = await DerivativesAPI.TranslateAsyncWithHttpInfo(job, bForce);
                HttpErrorHandler(response, "Failed to register file for SVF translation");
                return (true);
            }
            catch (Exception)
            {
                Console.WriteLine("**** Failed to register file for SVF translation");
                return (false);
            }
        }

        private async static Task<bool> Translate2Svf2(string urn, JobPayloadDestination.RegionEnum targetRegion = JobPayloadDestination.RegionEnum.US)
        {
            try
            {
                Console.WriteLine("**** Requesting SVF2 translation for: " + urn);
                JobPayloadInput jobInput = new JobPayloadInput(urn);
                JobPayloadOutput jobOutput = new JobPayloadOutput(
                    new List<JobPayloadItem>(
                        new JobPayloadItem[] {
                                new JobPayloadItem (
                                    JobPayloadItem.TypeEnum.Svf2,
                                    new List<JobPayloadItem.ViewsEnum> (
                                        new JobPayloadItem.ViewsEnum [] {
                                            JobPayloadItem.ViewsEnum._2d, JobPayloadItem.ViewsEnum._3d
                                        }
                                    ),
                                    null
                                )
                        }
                    ),
                    new JobPayloadDestination(targetRegion)
                );
                JobPayload job = new JobPayload(jobInput, jobOutput);
                bool bForce = true;
                ApiResponse<dynamic> response = await DerivativesAPI.TranslateAsyncWithHttpInfo(job, bForce);
                HttpErrorHandler(response, "Failed to register file for SVF2 translation");
                return (true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine("**** Failed to register file for SVF2 translation");
                return (false);
            }
        }

        private async static Task<bool> Translate2Obj(string urn, string guid, JobObjOutputPayloadAdvanced.UnitEnum unit = JobObjOutputPayloadAdvanced.UnitEnum.Meter, JobPayloadDestination.RegionEnum targetRegion = JobPayloadDestination.RegionEnum.US)
        {
            try
            {
                Console.WriteLine("**** Requesting OBJ translation for: " + urn);
                JobPayloadInput jobInput = new JobPayloadInput(urn);
                JobPayloadOutput jobOutput = new JobPayloadOutput(
                    new List<JobPayloadItem>(
                        new JobPayloadItem[] {
                                new JobPayloadItem (
                                    JobPayloadItem.TypeEnum.Obj,
                                    null,
									//new JobObjOutputPayloadAdvanced (null, guid, new List<int> () { -1 }, unit) // all
									new JobObjOutputPayloadAdvanced (null, guid, new List<int> () { 1526, 1527 }, unit)
                                )
                        }
                    ),
                    new JobPayloadDestination(targetRegion)
                );
                JobPayload job = new(jobInput, jobOutput);
                bool bForce = true;
                ApiResponse<dynamic> response = await DerivativesAPI.TranslateAsyncWithHttpInfo(job, bForce);
                HttpErrorHandler(response, "Failed to register file for OBJ translation");
                return (true);
            }
            catch (Exception)
            {
                Console.WriteLine("**** Failed to register file for OBJ translation");
                return (false);
            }
        }

        private async static Task<bool> Translate2Stl(string urn, string guid, JobObjOutputPayloadAdvanced.UnitEnum unit = JobObjOutputPayloadAdvanced.UnitEnum.Meter, JobPayloadDestination.RegionEnum targetRegion = JobPayloadDestination.RegionEnum.US)
        {
            try
            {
                Console.WriteLine("**** Requesting STL translation for: " + urn);
                JobPayloadInput jobInput = new(urn);
                JobPayloadOutput jobOutput = new(
                    new List<JobPayloadItem>(
                        new JobPayloadItem[] {
                                new JobPayloadItem (
                                    JobPayloadItem.TypeEnum.Stl,
                                    null,
                                    new JobStlOutputPayloadAdvanced (JobStlOutputPayloadAdvanced.FormatEnum.Ascii,
                                                    true, JobStlOutputPayloadAdvanced.ExportFileStructureEnum.Single)
                                )
                        }
                    ),
                    new JobPayloadDestination(targetRegion)
                );
                JobPayload job = new JobPayload(jobInput, jobOutput);
                bool bForce = true;
                ApiResponse<dynamic> response = await DerivativesAPI.TranslateAsyncWithHttpInfo(job, bForce);
                HttpErrorHandler(response, "Failed to register file for STL translation");
                return (true);
            }
            catch (Exception)
            {
                Console.WriteLine("**** Failed to register file for STL translation");
                return (false);
            }
        }

        private static bool DeleteManifest(string urn)
        {
            try
            {
                Console.WriteLine("**** Deleting manifest for: " + urn);
                DerivativesAPI.DeleteManifest(urn);
                return (true);
            }
            catch (Exception)
            {
                Console.WriteLine("**** Failed deleting manifest: " + urn);
                return (false);
            }
        }

        private static dynamic? GetManifest(string urn)
        {
            try
            {
                Console.WriteLine("**** Getting Manifest of: " + urn);
                dynamic response = DerivativesAPI.GetManifest(urn);
                return (response);
            }
            catch (Exception)
            {
                Console.WriteLine("**** Failed getting Manifest of: " + urn);
                return null; ;
            }
        }

        private static dynamic? GetMetadata(string urn)
        {
            try
            {
                Console.WriteLine("**** Getting Metadata of: " + urn);
                dynamic response = DerivativesAPI.GetMetadata(urn);
                return (response);
            }
            catch (Exception)
            {
                Console.WriteLine("**** Failed getting Metadata of: " + urn);
                return null; ;
            }
        }

        private static IDictionary<string, string>? GetDerivativesManifestHeaders(string urn, string derivativesUrn)
        {
            try
            {
                Console.WriteLine("**** Getting DerivativesManifest Headers of: " + derivativesUrn);
                dynamic response = DerivativesAPI.GetDerivativeManifestHeaders(urn, derivativesUrn);
                return (response);
            }
            catch (Exception)
            {
                Console.WriteLine("**** Failed getting DerivativesManifest Headers of: " + derivativesUrn);
                return null; ;
            }
        }

        private static dynamic? GetDerivativesManifest(string urn, string derivativesUrn)
        {
            try
            {
                Console.WriteLine("**** Getting DerivativesManifest of: " + derivativesUrn);
                dynamic response = DerivativesAPI.GetDerivativeManifest(urn, derivativesUrn);
                return (response);
            }
            catch (Exception)
            {
                Console.WriteLine("**** Failed getting DerivativesManifest of: " + derivativesUrn);
                return null; ;
            }
        }

        private static dynamic? FindDerivativesNode(string outputType, dynamic manifest)
        {
            if (manifest == null)
                return null; ;
            for (int i = 0; i < manifest.derivatives.Count; i++)
            {
                dynamic derivatives = manifest.derivatives[i];
                if (derivatives.outputType == outputType)
                    return (derivatives);
            }
            return null; ;
        }

        private static string? FindDerivativesNodeStatus(string outputType, dynamic manifest)
        {
            if (manifest == null)
                return null; ;
            dynamic derivatives = FindDerivativesNode(outputType, manifest);
            if (derivatives != null)
                return (derivatives.progress);
            return null; ;
        }

        #endregion

        #region US / EMEA processes

        public static async Task Clean(string urn, Region endpoint)
        {
            var derivativesAPI = (endpoint != Region.US) ? EMEADerivativesAPI : USDerivativesAPI;
            await CleanUP(urn, derivativesAPI);

        }
        private static async Task CleanUP(string urn, DerivativesApi endpoint)
        {
            Console.WriteLine($"Running Endpoint:{(endpoint.RegionIsEMEA ? "EMEA" : "US")}");            
            DerivativesAPI = endpoint;
            dynamic? response = await OauthExecAsync();
            if (response == null) return;
            DeleteManifest(urn);
            // This deletes the file(s) as well
            DeleteBucket(); 
        }

        public static async Task RunWorkFlow()
        {
           
            Console.WriteLine(nameof(RunWorkFlow));
            try
            {
                string? filePath = ConfigOptions.InputPath;
                if (filePath == null)
                {

                    throw new ArgumentNullException($"{nameof(filePath)} is null");

                }
                string? serverEndpoint = ConfigOptions.ServerEndpoint;                
                if (serverEndpoint == null)
                {

                    throw new ArgumentNullException($"{nameof(serverEndpoint)} is null");

                }
                string? storageRegion = ConfigOptions.StorageRegion;                
                if (storageRegion == null)
                {

                    throw new ArgumentNullException($"{nameof(storageRegion)} is null");

                }
                string? targetRegion = ConfigOptions.DerivativeRegion;                
                if (targetRegion == null)
                {

                    throw new ArgumentNullException($"{nameof(targetRegion)} is null");

                }               
                FileInfo file = new FileInfo(filePath);
                ObjectKey = Path.GetFileName(file.FullName);
                Console.WriteLine($"{BucketKey}/{ObjectKey}");
                string? urn = BuildURN(BucketKey, ObjectKey);
                Console.WriteLine(urn);
                Region? endpoint = Enum.Parse<Region>(serverEndpoint, true);
                Region storage = Enum.Parse<Region>(storageRegion, true);
                JobPayloadDestination.RegionEnum target = Enum.Parse<JobPayloadDestination.RegionEnum>(targetRegion, true);
                Console.WriteLine(filePath);
                Console.WriteLine(file.FullName);
                urn = await TryWorkflow(
                   file,
                   endpoint == Region.US ? USDerivativesAPI : EMEADerivativesAPI,
                   storage,
                   target

               );
              Console.WriteLine($"Note urn :{urn} if you want to delete manifest with `forgemd clean --urn` command");
            }
            catch (Exception) { throw; }
        }

        private static async Task<string?> GenerateObjWorkFlow(string found, dynamic? response, string urn)
        {
            found = FindDerivativesNodeStatus("obj", response);
            string? guid = null;
            if (found == null)
            {
                response = GetMetadata(urn);
                if (response == null)
                    return urn;
                guid = response.data.metadata[0].guid;
                string? targetRegion = ConfigOptions.DerivativeRegion;
                if (targetRegion == null)
                {

                    throw new ArgumentNullException($"{nameof(targetRegion)} is null");

                }              
                JobPayloadDestination.RegionEnum target = Enum.Parse<JobPayloadDestination.RegionEnum>(targetRegion, true);
                response = await Translate2Obj(urn, guid, JobObjOutputPayloadAdvanced.UnitEnum.Meter, target);
                 if (!response)
                     return (urn);
                 Console.WriteLine("Please wait for OBJ translation to complete");
                return urn;
            }
            else if (found != "complete")
            {
                Console.WriteLine("Translation Failed or Still translating...");
                return urn;
            }

            dynamic node = FindDerivativesNode("obj", response);

            response = GetMetadata(urn);
            if (response == null)
                return urn;

            guid = response.data.metadata[0].guid;

            for (int i = 0; i < node.children.Count; i++)
            {
                dynamic elt = node.children[i];
                if (elt.type != "resource" || elt.status != "success" || elt.modelGuid != guid || elt.role != "obj")
                    continue;
                Console.WriteLine(elt.urn);

                IDictionary<string, string> headers = GetDerivativesManifestHeaders(urn, elt.urn);
                Console.WriteLine("\t size: " + headers["Content-Length"]);

                System.IO.MemoryStream stream = GetDerivativesManifest(urn, elt.urn);
                if (stream == null)
                    continue;
                stream.Seek(0, SeekOrigin.Begin);
                string name = elt.urn.Substring(elt.urn.LastIndexOf('/') + 1);
                File.WriteAllBytes(name, stream.ToArray());
            }
            return urn;
        }

        private static async Task<string?> TryWorkflow(FileInfo file, DerivativesApi endpoint, Region storage = Region.US, JobPayloadDestination.RegionEnum derivativeRegion = JobPayloadDestination.RegionEnum.US)
        {

            Console.WriteLine("Running Endpoint: " + (endpoint.RegionIsEMEA ? "EMEA" : "US") + " server - Storage: " + storage.ToString() + " - Derivative Region: " + derivativeRegion.ToString());
            DerivativeLocation = storage;
            DerivativesAPI = endpoint;

            dynamic? response = await OauthExecAsync();
            if (response == null)
                return null; ;
            response = CreateBucketIfNotExist();
            if (!response)
                return null; ;
            //DeleteSampleFile();

            ObjectKey = Path.GetFileName(file.Name);
            response = UploadSampleFile(input: file.FullName);
            if (response == null)
                return null; ;
            string urn = SafeBase64Encode(response);
            if (!await Translate2Svf2(urn, derivativeRegion)) return null;
            do
            {
                response = GetManifest(urn);
                await Task.Delay(2000);
                Console.WriteLine($"\n{response?.progress}...."); 
            } while (response?.progress != "complete");
            Console.WriteLine("Done");            
            return (urn);
        }

        #endregion

        private static async Task<int> Main(string[] args)
        {

            Console.WriteLine(Figgle.FiggleFonts.Standard.Render("FORGE"));            
            var root = new CommandBuilder().Build();
            return await root.InvokeAsync(args);            
        }

        #region Utils
        public static bool HttpErrorHandler(ApiResponse<dynamic> response, string msg = "", bool bThrowException = true)
        {
            if (response.StatusCode < 200 || response.StatusCode >= 300)
            {
                if (bThrowException)
                    throw new Exception(msg + " (HTTP " + response.StatusCode + ")");
                return (true);
            }
            return (false);
        }

        private static readonly char[] padding = { '=' };
        public static string SafeBase64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return (System.Convert.ToBase64String(plainTextBytes)
                .TrimEnd(padding).Replace('+', '-').Replace('/', '_')
            );
        }

        public static string SafeBase64Decode(string base64EncodedData)
        {
            string st = base64EncodedData.Replace('_', '/').Replace('-', '+');
            switch (base64EncodedData.Length % 4)
            {
                case 2:
                    st += "==";
                    break;
                case 3:
                    st += "=";
                    break;
            }
            var base64EncodedBytes = System.Convert.FromBase64String(st);
            return (System.Text.Encoding.UTF8.GetString(base64EncodedBytes));
        }

        public static string BuildURN(string bucketKey, string objectKey)
        {
            return (SafeBase64Encode($"urn:adsk.objects:os.object:{bucketKey}/{objectKey}"));
        }

        #endregion
    }
}