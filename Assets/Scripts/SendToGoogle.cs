using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using UnityEngine.SceneManagement;

public class SendToGoogle : MonoBehaviour
{
    [SerializeField] private string URL = "https://docs.google.com/forms/d/e/1FAIpQLSd-S6YVYOZnN_6exc5vZ5VZo5YNYqJp_lvhJsBtB9ELpxqVrQ/formResponse";
    
    // 检查点计算相关的数据，我们的计算方法非常简单粗暴，人物重生的位置 -> Finish Flag
    // Note: 可能有些关卡检查点后面还有一点东西，我的建议是，直接忽略。。。
    private float playerRespawnPosX; // 人物重生的位置
    private float finishFlagPosX; // 结束标识的位置
    private float distanceBetweenCheckpoint; // 相邻两个检查点之间的距离
    private float rangeAtEachCheckpoint = 0.5f; // 每个检查点前后正负的范围，默认为正负0.5
    
    // 检查点数据的数组
    private static int[] surviveAtEachCheckPoint;
    private static int[] timeSpentAtEachCheckPoint;
    private static int[] remainingHealthAtEachCheckPoint;
    private static int[] jumpCountAtEachCheckPoint;
    
    // 检查点数据的锁
    private static bool[] surviveAtEachCheckPointRecord;
    private static bool[] timeSpentAtEachCheckPointRecord;
    private static bool[] jumpCountAtEachCheckPointRecord;
    
    private static int startTime;
    private static int deathCount;
    
    // Http链接
    private String _sessionID;
    private int _testInt;
    private bool _testBool;
    private float _testFloat;
    
    // 数据 何瑛琪 负责部分
    private int _mechanismSection;
    private String _level;
    private int _attempt;
    private int _complete;
    private int _timeSpent;
    private int _retriesNumber;
    
    // 数据 王思极 & 徐通负责部分
    // 第一次
    private int _surviveC1; // 改成累计死亡次数，这个checkpoint前的死亡次数
    private int _surviveC2;
    private int _surviveC3;
    private int _surviveC4;
    private int _surviveC5;
    // 第一次
    private int _timeSpentC1; // 时间也是累计
    private int _timeSpentC2;
    private int _timeSpentC3;
    private int _timeSpentC4;
    private int _timeSpentC5;
    // 最后一次
    private int _remainingHealthC1;
    private int _remainingHealthC2;
    private int _remainingHealthC3;
    private int _remainingHealthC4;
    private int _remainingHealthC5;
    // 第一次
    private static int _jCount;
    private int _jCountC1;
    private int _jCountC2;
    private int _jCountC3;
    private int _jCountC4;
    private int _jCountC5;
    
    // 数据 吴奕宙 负责部分
    private int _losePointsReason;
    
    private void Awake()
    {
        // 用当前的时间创建唯一的会话ID
        _sessionID = DateTime.Now.Ticks.ToString();
    }
    
    // Start is called before the first frame update
    void Start()
    {
        // 初始化检查点相关的变量
        playerRespawnPosX = GameObject.Find("AstroStay").transform.position.x;
        finishFlagPosX = GameObject.FindWithTag("FinishFlag").transform.position.x;
        distanceBetweenCheckpoint = (finishFlagPosX - playerRespawnPosX) / 5f;
        
        // 初始化数据
        initializeData();
        GetLevelInfo();
        _attempt = 1;
        
        // 创建起始时间
        startTime = (int)Time.time;
        deathCount = 0;
    }

    // Update is called once per frame
    void Update()
    {
        // 统计按J的次数
        if (Input.GetKeyDown(KeyCode.J))
        {
            ++_jCount;
        }
        
        if (Input.GetKeyDown(KeyCode.J))
        {
            ++deathCount;
        }
        
        // check一下是否在检查点周围，如果是就统计数据
        CalDataAtEachCheckpoint();
        
        // 测试用，按P发送数据
        // 两位可以用这个来测试
        // 数据表格是 https://docs.google.com/spreadsheets/d/1Z2gvKMsN287wGSLa7DW8BbaGF2K0u2nop29Y86-fMbA/edit?resourcekey#gid=1659411542
        // if (Input.GetKeyDown(KeyCode.P))
        // {
        //     Send();
        // }
        
        // 监听选关按钮
        // TODO: 存在bug 会一次性发送几百个，Yizhou可以帮我看看这个bug
        // GameObject.Find("menu_btn").GetComponent<Button>().onClick.AddListener(() => Send());
    }
    
    // 碰到Finish flag的时候发送表单
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name == "FinishFlag")
        {
            DataAtFinishFlag();
            Send();
            deathCount = 0;
        }
    }

    // 计算每个检查点的跳跃次数
    private void CalDataAtEachCheckpoint()
    {
        // 获取人物当前的位置
        float currPlayerPos = GameObject.Find("AstroStay").transform.position.x;
        for (int i = 1; i <= 5 ; i++)
        {
            // 在某个检查点附近的时候
            if (currPlayerPos >= playerRespawnPosX + i * distanceBetweenCheckpoint - 0.5 &&
                currPlayerPos < playerRespawnPosX + i * distanceBetweenCheckpoint + 0.5)
            {
                // 统计一下当前死亡次数
                if (!surviveAtEachCheckPointRecord[i - 1])
                {
                    surviveAtEachCheckPoint[i - 1] = GameController.deathCount;
                    surviveAtEachCheckPointRecord[i - 1] = true;
                }
                // 统计一下当前花费的时间
                if (!timeSpentAtEachCheckPointRecord[i - 1])
                {
                    timeSpentAtEachCheckPoint[i - 1] = (int)Time.time - startTime;
                    timeSpentAtEachCheckPointRecord[i - 1] = true;
                }
                // 统计一下当前的生命值
                remainingHealthAtEachCheckPoint[i - 1] = GameObject.FindWithTag("Player").GetComponent<HealthBarForPlayer>().health;
                // 统计一下当前按J的次数
                if (!jumpCountAtEachCheckPointRecord[i - 1])
                {
                    jumpCountAtEachCheckPoint[i - 1] = _jCount;
                    jumpCountAtEachCheckPointRecord[i - 1] = true;
                }
                break;
            }
        }
    }
    
    // 将每个检查点的数据放到对应的变量中
    // 这个函数写的非常愚蠢，但我不想删
    private void PutDataAtEachCheckpoint()
    {
        _surviveC1 = surviveAtEachCheckPoint[0];
        _surviveC2 = surviveAtEachCheckPoint[1];
        _surviveC3 = surviveAtEachCheckPoint[2];
        _surviveC4 = surviveAtEachCheckPoint[3];
        _surviveC5 = surviveAtEachCheckPoint[4];
        _timeSpentC1 = timeSpentAtEachCheckPoint[0];
        _timeSpentC2 = timeSpentAtEachCheckPoint[1];
        _timeSpentC3 = timeSpentAtEachCheckPoint[2];
        _timeSpentC4 = timeSpentAtEachCheckPoint[3];
        _timeSpentC5 = timeSpentAtEachCheckPoint[4];
        _remainingHealthC1 = remainingHealthAtEachCheckPoint[0];
        _remainingHealthC2 = remainingHealthAtEachCheckPoint[1];
        _remainingHealthC3 = remainingHealthAtEachCheckPoint[2];
        _remainingHealthC4 = remainingHealthAtEachCheckPoint[3];
        _remainingHealthC5 = remainingHealthAtEachCheckPoint[4];
        _jCountC1 = jumpCountAtEachCheckPoint[0];
        _jCountC2 = jumpCountAtEachCheckPoint[1];
        _jCountC3 = jumpCountAtEachCheckPoint[2];
        _jCountC4 = jumpCountAtEachCheckPoint[3];
        _jCountC5 = jumpCountAtEachCheckPoint[4];
    }

    //hyq
    private void GetLevelInfo()
    {
        Scene scene = SceneManager.GetActiveScene();
        _level = scene.name;
        int sceneID = scene.buildIndex;
        if (sceneID >= 7 && sceneID <= 12)
        {
            _mechanismSection = 1;
        }
        else if (sceneID >= 13 && sceneID <= 20)
        {
            _mechanismSection = 2;
        }
        else if (sceneID >= 21 && sceneID <= 25)
        {
            _mechanismSection = 3;
        }
        else if (sceneID >= 26)
        {
            _mechanismSection = 4;
        }
    }
    
    private void DataAtFinishFlag()
    {
        _timeSpent = (int)Time.time - startTime;
        _retriesNumber = deathCount;
        _complete = 1;
    }

    // 初始化数据
    private void initializeData()
    {
        // hyq
        _mechanismSection = 0;
        _level = "0";
        _attempt = 0;
        _complete = 0;
        _timeSpent = -2;
        _retriesNumber = -1;
        
        // 数据 王思极 & 徐通负责部分
        _jCount = 0;
        // 初始化各个检查点的数组
        surviveAtEachCheckPoint = new[] { -2, -2, -2, -2, -2 };
        timeSpentAtEachCheckPoint = new[] { -2, -2, -2, -2, -2 };
        remainingHealthAtEachCheckPoint = new[] { -2, -2, -2, -2, -2 };
        jumpCountAtEachCheckPoint = new[] { -2, -2, -2, -2, -2 };
        
        // 初始化各个检查点数据的锁
        surviveAtEachCheckPointRecord = new[] { false, false, false, false, false };
        timeSpentAtEachCheckPointRecord = new[] { false, false, false, false, false };
        jumpCountAtEachCheckPointRecord = new[] { false, false, false, false, false };
    }
    
    // 发送数据
    private void Send()
    {
        PutDataAtEachCheckpoint();
        StartCoroutine(Post());
    }
    
    // Post Http request
    private IEnumerator Post()
    {
        WWWForm form = new WWWForm();
        form.AddField("entry.694268104", _mechanismSection);
        form.AddField("entry.1179097589", _level);
        form.AddField("entry.1585400280", _sessionID);
        form.AddField("entry.199638597", _attempt);
        form.AddField("entry.188062634", _complete);
        form.AddField("entry.1478498527", _timeSpent);
        form.AddField("entry.820838070", _retriesNumber);
        form.AddField("entry.1450257209", _surviveC1);
        form.AddField("entry.1364728483", _surviveC2);
        form.AddField("entry.231225115", _surviveC3);
        form.AddField("entry.73692411", _surviveC4);
        form.AddField("entry.1754632933", _surviveC5);
        form.AddField("entry.1245109354", _timeSpentC1);
        form.AddField("entry.628811664", _timeSpentC2);
        form.AddField("entry.424709657", _timeSpentC3);
        form.AddField("entry.802975287", _timeSpentC4);
        form.AddField("entry.178535423", _timeSpentC5);
        form.AddField("entry.498993239", _remainingHealthC1);
        form.AddField("entry.927801927", _remainingHealthC2);
        form.AddField("entry.1301255164", _remainingHealthC3);
        form.AddField("entry.529486672", _remainingHealthC4);
        form.AddField("entry.588232958", _remainingHealthC5);
        form.AddField("entry.1530851387", _losePointsReason);
        form.AddField("entry.107226697", _jCount);
        form.AddField("entry.2003297119", _jCountC1);
        form.AddField("entry.1767977406", _jCountC2);
        form.AddField("entry.650173284", _jCountC3);
        form.AddField("entry.975144134", _jCountC4);
        form.AddField("entry.1994227933", _jCountC5);

        // Send responses and verify result
        using (UnityWebRequest www = UnityWebRequest.Post(URL, form))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("Form upload complete!");
            }
        }
    }
}