using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;

namespace CloudStorageManager
{
    public static class BatchUtilities
    {
        private const string PoolId = "FileUnziper";
        private const string JobId = "UnzipFile";

        // Create a BatchClient. We'll now be interacting with the Batch service in addition to Storage
        private static BatchClient _resourceClient;

        public static BatchClient ResourceClient
        {
            get
            {
                if (_resourceClient == null)
                {
                    string BatchAccountName = "coman";
                    string BatchAccountKey =
                        "mpgIXIkoJdBUoHlqVAKs+QbXJzN9rZTPhfDG6kuoRaVyDZouYDE5WRZjmFUQdHir6fAhxezLz5OfWcUNR40PoA==";
                    string BatchAccountUrl = "https://coman.westeurope.batch.azure.com";
                    BatchSharedKeyCredentials cred = new BatchSharedKeyCredentials(BatchAccountUrl, BatchAccountName,
                        BatchAccountKey);
                    _resourceClient = BatchClient.Open(cred);
                }
                return _resourceClient;
            }
        }

        private static async Task<List<CloudTask>> AddTasksAsync(BatchClient batchClient, string jobId,
            List<ResourceFile> inputFiles, string outputContainerSasUrl)
        {
            // Create a collection to hold the tasks that we'll be adding to the job
            List<CloudTask> tasks = new List<CloudTask>();

            // Create each of the tasks. Because we copied the task application to the
            // node's shared directory with the pool's StartTask, we can access it via
            // the shared directory on whichever node each task will run.
            foreach (ResourceFile inputFile in inputFiles)
            {
                string taskId = "topNtask" + inputFiles.IndexOf(inputFile);
                string taskCommandLine =
                    String.Format("cmd /c %AZ_BATCH_NODE_SHARED_DIR%\\TaskApplication.exe {0} 3 \"{1}\"",
                        inputFile.FilePath, outputContainerSasUrl);

                CloudTask task = new CloudTask(taskId, taskCommandLine);
                task.ResourceFiles = new List<ResourceFile> {inputFile};
                tasks.Add(task);
            }

            await batchClient.JobOperations.AddTaskAsync(jobId, tasks);

            return tasks;
        }

        public static async Task CreateResourceJobAsync(string jobId = "", string poolId = "")
        {
            if (poolId == string.Empty)
                poolId = BatchUtilities.PoolId;
            if (jobId == string.Empty)
                jobId = BatchUtilities.JobId;

            await CreateJobAsync(ResourceClient, jobId, poolId);
            await ResourceClient.CloseAsync();
        }

        private static async Task CreateJobAsync(BatchClient batchClient, string jobId, string poolId)
        {
            DebugInfo.Log($"Creating job with id{jobId} on pool {poolId}");
            CloudJob job = batchClient.JobOperations.CreateJob();
            job.Id = jobId;
            job.PoolInformation = new PoolInformation {PoolId = poolId};
            DebugInfo.Log($"Created job with id{jobId} on pool {poolId}");
            await job.CommitAsync();
        }

        public static async Task CreateResourcePoolAsync(IList<ResourceFile> resourceFile, string PoolId = "")
        {
            if (PoolId == string.Empty)
                PoolId = BatchUtilities.PoolId;

            await CreatePoolAsync(ResourceClient, PoolId, resourceFile);
            await ResourceClient.CloseAsync();
        }

        private static async Task CreatePoolAsync(BatchClient batchClient, string poolId,
            IList<ResourceFile> resourceFiles, int targetDedicated = 1)
        {
            DebugInfo.Log($"Creating pool with id{PoolId}");

            // Create the unbound pool. Until we call CloudPool.Commit() or CommitAsync(), no pool is actually created in the
            // Batch service. This CloudPool instance is therefore considered "unbound," and we can modify its properties.
            CloudPool pool = batchClient.PoolOperations.CreatePool(
                poolId: poolId,
                targetDedicated: targetDedicated, // 1 compute nodes
                virtualMachineSize: "extraLarge", // single-core, 1.75 GB memory, 225 GB disk
                cloudServiceConfiguration: new CloudServiceConfiguration(osFamily: "4")); // Windows Server 2012 R2

            pool.StartTask = new StartTask
            {
                CommandLine =
                    "cmd /c (robocopy %AZ_BATCH_TASK_WORKING_DIR% %AZ_BATCH_NODE_SHARED_DIR%) ^& IF %ERRORLEVEL% LEQ 1 exit 0",
                ResourceFiles = resourceFiles,
                WaitForSuccess = true
            };

            DebugInfo.Log($"Created pool with id{PoolId}");
            await pool.CommitAsync();
        }
    }
}