using UnityEngine; 
using System.Collections;
using System.Collections.Generic;
 
public enum TaskType
{
    Coffe,
    Food,
    CleaningToilet,
    TakingPiss,
    CustomerOrder,
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
    public static TaskManager Instance { get; private set; }

    [SerializeField]
    public List<TaskItem> taskList = new List<TaskItem>();
    public List<Job> currentJobs = new List<Job>();
    public Job lastJob;
    
    public int maxJobs = 5;
    public float timeBetweenJobs = 25f;
    public float timeLeftForJob = 0f;
    int jobIdCounter = 0;
    public UIHandler uiHandler;
    [Header("Cafe customer bootstrap")]
    [SerializeField] private Texture2D travellerSpriteSheet;
    [SerializeField] private Sprite[] beverageIcons;
    [SerializeField] private BeverageDefinition[] cafeBeverages;
    [Header("Money")]
    [SerializeField, Min(0)] private int startingMoney = 0;
    [SerializeField, Min(0)] private int moneyPerOrder = 5;
    private readonly List<CustomerOrderTask> customerOrders = new List<CustomerOrderTask>();
    private int customerOrderIdCounter;
    private int completedOrderCount;
    private int money;
    private int completedCustomerOrders;
    private int catDeliveredCount;
    private int dogDeliveredCount;

    public IReadOnlyList<CustomerOrderTask> CustomerOrders => customerOrders;
    public IReadOnlyList<BeverageDefinition> CafeBeverages => cafeBeverages;
    public int Money => money;
    public int CompletedCustomerOrders => completedCustomerOrders;
    public int CompletedOrderCount => completedOrderCount;
    public int MoneyEarned => Mathf.Max(0, money - startingMoney);
    public int CatDeliveredCount => catDeliveredCount;
    public int DogDeliveredCount => dogDeliveredCount;

    public void ResetRoundStatistics()
    {
        money = startingMoney;
        completedOrderCount = 0;
        completedCustomerOrders = 0;
        catDeliveredCount = 0;
        dogDeliveredCount = 0;
        uiHandler?.SetMoney(money);
    }

    private void Awake()
    {
        Instance = this;
        money = startingMoney;
        if (beverageIcons != null && beverageIcons.Length > 0)
        {
            cafeBeverages = CafeRuntimeSetup.CreateBeverageMenu(beverageIcons, 20);
        }
    }

    public BeverageDefinition[] GetCafeBeverages()
    {
        return cafeBeverages;
    }

    public int AddCustomerOrder(FrogCustomer customer, BeverageDefinition beverage)
    {
        int id = customerOrderIdCounter++;
        customerOrders.Add(new CustomerOrderTask(id, customer, beverage));
        RefreshCustomerOrderUi();
        return id;
    }

    public void CompleteCustomerOrder(int id, BeverageServer server)
    {
        int removed = customerOrders.RemoveAll(order => order.id == id);
        if (removed > 0)
        {
            completedOrderCount++;
            money += moneyPerOrder;
            completedCustomerOrders += removed;
            if (server == BeverageServer.Cat)
            {
                catDeliveredCount += removed;
            }
            else
            {
                dogDeliveredCount += removed;
            }
            if (uiHandler != null)
            {
                uiHandler.SetMoney(money);
            }
        }
        RefreshCustomerOrderUi();
    }

    public void CancelCustomerOrder(int id)
    {
        customerOrders.RemoveAll(order => order.id == id);
        RefreshCustomerOrderUi();
    }

    private void RefreshCustomerOrderUi()
    {
        if (uiHandler != null && uiHandler.isActiveAndEnabled)
        {
            uiHandler.RefreshCustomerOrders(customerOrders);
        }
    }

    public BeverageType GetOldestCustomerOrderType()
    {
        return customerOrders.Count > 0 ? customerOrders[0].beverage.type : BeverageType.None;
    }
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
            yield return new WaitUntil(() => GameManager.IsGameActiveNow);

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

    public void ResetRound()
    {
        StopAllCoroutines();
        currentJobs.Clear();
        customerOrders.Clear();
        lastJob = null;
        jobIdCounter = 0;
        customerOrderIdCounter = 0;
        completedOrderCount = 0;
        completedCustomerOrders = 0;
        catDeliveredCount = 0;
        dogDeliveredCount = 0;
        money = startingMoney;

        uiHandler?.CleanJobList();
        uiHandler?.SetMoney(money);
        RefreshCustomerOrderUi();
        StartCoroutine(StartRandomTasks());
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
        CafeRuntimeSetup.Ensure(travellerSpriteSheet, cafeBeverages);
        FindAnyObjectByType<BeverageFridge>()?.Configure(cafeBeverages);
        uiHandler?.CleanJobList();
        uiHandler?.SetMoney(money);
        uiHandler?.RefreshCustomerOrders(customerOrders);
        StartCoroutine(StartRandomTasks());
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

[System.Serializable]
public sealed class CustomerOrderTask
{
    public int id;
    public FrogCustomer customer;
    public BeverageDefinition beverage;

    public CustomerOrderTask(int id, FrogCustomer customer, BeverageDefinition beverage)
    {
        this.id = id;
        this.customer = customer;
        this.beverage = beverage;
    }
}
