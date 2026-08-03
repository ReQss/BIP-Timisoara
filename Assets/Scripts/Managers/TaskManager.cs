using UnityEngine; 
using System.Collections;
using System.Collections.Generic;
 
public enum TaskType
{
    Coffe,
    Food,
    CleaningToilet,
    TakingPiss,
}
[System.Serializable]
public class TaskItem
{
    public string taskName;
    public string taskDescription;
    public TaskType taskType;
}
[System.Serializable]
public class Job
{
    public int waitingTime;
    public TaskItem assignedTask;
    public int jobId;
    public Job(int waitingTime, TaskItem assignedTask, int jobId)
    {
        this.waitingTime = waitingTime;
        this.assignedTask = assignedTask;
        this.jobId = jobId;
    }
}

public class TaskManager : MonoBehaviour
{
    [SerializeField]
    public List<TaskItem> taskList = new List<TaskItem>();
    public List<Job> currentJobs = new List<Job>();
    public Job lastJob;
    
    public int maxJobs = 5;
    public float timeBetweenJobs = 25f;
    public float timeLeftForJob = 0f;
    int jobIdCounter = 0;
    public UIHandler uiHandler;
    public Job GetRandomJob()
    {
        //waiting time random from 30 to 60s
        TaskItem randomTask = GetRandomTask();
        int waitingTime;
        if(randomTask.taskType == TaskType.CleaningToilet)
        {
            waitingTime = 400;
        }
        else
        {
            waitingTime = Random.Range(30, 60);
        }
        Job newJob = new Job(waitingTime, randomTask, jobIdCounter++);
        return newJob;
    }
    public TaskItem GetRandomTask()
    {
        int randomIndex = Random.Range(0, taskList.Count);
        return taskList[randomIndex];
    }
    public IEnumerator StartRandomTasks()
    {
        while (true)
        {
            Job newJob = GetRandomJob();
            if(currentJobs.Count < maxJobs)
            {
                currentJobs.Add(newJob);
                lastJob = newJob;
                uiHandler.AddJobTextToList(newJob);
                StartCoroutine(RemoveJob(newJob));
            }
            else
            {
                //wait until a customer is removed from the list
                yield return new WaitUntil(() => currentJobs.Count < maxJobs);

            }
            yield return new WaitForSeconds(timeBetweenJobs);

        }
    }
    public IEnumerator RemoveJob(Job jobToRemove)
    {
        //wait for the job to be completed
        yield return new WaitUntil(() => jobToRemove.waitingTime <= 0);
        currentJobs.Remove(jobToRemove);
        uiHandler.RemoveJobTextFromList(jobToRemove.jobId);
        yield return null;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        uiHandler.CleanJobList();
        StartCoroutine(StartRandomTasks());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
