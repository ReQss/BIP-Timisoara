using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class JobItemInList
{
    public Job job = null;
    public TextMeshProUGUI jobDescription;
    public TextMeshProUGUI timeLeft;
}
public class UIHandler : MonoBehaviour
{
    public GameObject pauseMenu;
    //5 elements for jobs
    public List<JobItemInList> jobItemsInList = new List<JobItemInList>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            pauseMenu.SetActive(!pauseMenu.activeSelf);
        }
    }
    public JobItemInList GetFirstFreeJobItemInList()
    {
        for(int i = 0; i < jobItemsInList.Count; i++)
        {
            if(jobItemsInList[i].job == null || jobItemsInList[i].jobDescription.text == "")
            {
                return jobItemsInList[i];
            }
        }
        return null;
    }
    public void AddJobTextToList(Job job)
    {
        JobItemInList newJobItem = GetFirstFreeJobItemInList();
        if(newJobItem == null)
        {
            Debug.LogWarning("No free job item in list to add job text");
            return;
        }
        newJobItem.job = job;
        newJobItem.jobDescription.text = job.assignedTask.taskDescription;
        newJobItem.timeLeft.text = job.waitingTime.ToString();
    }
    
       
    public void CleanJobList()
    {
        foreach (var jobItem in jobItemsInList)
        {
            jobItem.job = null;
            jobItem.jobDescription.text = "";
            jobItem.timeLeft.text = "";
        }
    }
    public void RemoveJobTextFromList(int jobId)
    {
        JobItemInList jobToRemove = jobItemsInList.Find(job => job.job != null && job.job.jobId == jobId);
        if (jobToRemove != null)
        {
            jobToRemove.job = null;
            jobToRemove.jobDescription.text = "";
            jobToRemove.timeLeft.text = "";
        }
        //reorganize the list and push items to the top of the list
        //sort by job id
        jobItemsInList.Sort((a, b) => 
        {
            if (a.job == null && b.job == null) return 0;
            if (a.job == null) return 1;
            if (b.job == null) return -1;
            return a.job.jobId.CompareTo(b.job.jobId);
        });
    }
    public void LoadScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
    public void ActiveOrDisablePanel(GameObject panel)
    {
        panel.SetActive(!panel.activeSelf);
    }
}
