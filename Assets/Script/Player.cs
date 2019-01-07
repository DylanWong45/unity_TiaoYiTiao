using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;//场景
using UnityEngine.UI;//分数
using DG.Tweening;//相机跟随插件
using System;
using UniRx;
using LeanCloud;
using Random = UnityEngine.Random;


public class Player : MonoBehaviour
{
    //跳跃因数 每秒跳跃距离
    public float Factor=4;

    //生成跳台的最大距离
    public float MaxDistance=2f;

    //获取第一个跳台
    public GameObject Stage;

    // 盒子仓库，可以放上各种盒子的prefab，用于动态生成。
    public GameObject[] BoxTemplates;

    //蓄力效果
    public GameObject Particle;

    //小人头部 身体（蓄力效果）
    public Transform Head;
    public Transform Body;

    //获取相机位置
    public Transform Camera;

    //计分板
    public Text ScoreText;      //得分总和
    public Text SingleScoreText;//单次得分

    //音效控制按钮
    public Button SoundsButton;
    public Text SoundsText;

    //重新开始按钮
    public Button ConstRestartButton;

    //开始界面
    public GameObject StartPanel;
    public Button StratButton;
    public Button ShowListButton;
    public Button ShowLoginPanel;
    public Button ShowRegisterPanel;
    public Text Users;

    //上传界面
    public GameObject SaveScorePanel;
    public Text TotalScore_S;
    public Button SaveButton;
    public Button RestartButton;

    //登陆界面
    public GameObject LoginPanel;
    public Button ReturnButton1;
    public Button LoginButton;
    public InputField Username_L;
    public InputField Password_L;


    //注册界面
    public GameObject RegisterPanel;
    public Button ReturnButton2;
    public Button RegisterButton;
    public InputField Username_R;
    public InputField Password_R;
    public InputField Password2_R;

    //消息
    public GameObject Inf;
    public Text InfText;
    public Button InfButton;

    //排行榜
    public GameObject RankPanel;
    public GameObject RankItem;
    public Button RestartButton2;

    //飘分时是否更新位置
    private bool _isUpdateScoreAnimation = false;
    private float _scoreAnimationStartTime;

    //刚体
    private Rigidbody _rigidbody;

    //起始时间
    private float _startTime;

    //当前小人所在的跳台
    private GameObject _currentStage;

    //上一次碰撞的跳台
    private Collider _lastCollisionCollider;

    //相机相对位置
    private Vector3 _cameraRelativePosition;

    //分数
    private int _score;

    //输入使能控制
    private bool _enableInput;

    //最近的得分
    private int _lastReward = 1;

    //云端用户数据
    private LeanCloud.AVUser _nowUser;
    private static string _nowUsername = "游客";

    //初始化沿x轴正方向生成跳台
    Vector3 _direction = new Vector3(1, 0, 0);

    //音频
    public AudioClip music;
    private static bool isOnPlay = true;
    private AudioSource _audioSource;

    //
    // 初始化
    //
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();

        //粒子效果
        Particle = GameObject.Find("Yellow");
        Particle.SetActive(false);

        //降低重心至物体底部
        _rigidbody.centerOfMass = Vector3.zero;

        _currentStage = Stage;
        _lastCollisionCollider=_currentStage.GetComponent<Collider>();
        SpawnStage();

        //相机相对位置=相机位置-小人位置
        _cameraRelativePosition = Camera.position - transform.position;

        //
        //给保存按钮绑定事件
        //

        //开始界面按钮
        ShowLoginPanel.onClick.AddListener(OnClickShowLoginPanel);
        ShowRegisterPanel.onClick.AddListener(OnClickShowRegisterPanel);
        ShowListButton.onClick.AddListener(OnClickShowListButton);
        StratButton.onClick.AddListener(OnClickStartButton);
        //上传界面按钮
        SaveButton.onClick.AddListener(OnClickSaveButton);
        RestartButton.onClick.AddListener(OnClickRestartButton);
        //排行榜界面按钮
        RestartButton2.onClick.AddListener(OnClickRestartButton);
        //注册登陆界面按钮
        ReturnButton1.onClick.AddListener(OnClickReturnButton1);
        ReturnButton2.onClick.AddListener(OnClickReturnButton2);
        LoginButton.onClick.AddListener(OnClickLoginButton);
        RegisterButton.onClick.AddListener(OnClickRegisterButton);
        //消息响应窗口按钮
        InfButton.onClick.AddListener(() =>
        {
            Inf.SetActive(false);
        });

        SoundsButton.onClick.AddListener(OnClickSoundsButton);
        ConstRestartButton.onClick.AddListener(OnClickRestartButton);

        //初始化
        Users.text = "当前用户：" + _nowUsername;
        _enableInput = false;
        ScoreText.gameObject.SetActive(true);
        StartPanel.SetActive(true); 
        MainThreadDispatcher.Initialize();

        _audioSource = this.gameObject.AddComponent<AudioSource>();
        _audioSource.clip = music;

        if (isOnPlay)
        {
            _audioSource.Play();
            SoundsText.text = "音效：开";
        }
        else
        {
            _audioSource.Pause();
            SoundsText.text = "音效：关";
        }


    }


    //
    // 更新
    //
    void Update()
    {
        if (_enableInput)
        {
            //按下空格
            if (Input.GetKeyDown(KeyCode.Space))
            {
                //此刻
                _startTime = Time.time;

                //按下空格出现粒子效果
                Particle.SetActive(true);
            }

            //松开空格
            if (Input.GetKeyUp(KeyCode.Space))
            {
                //时间差
                var elapse = Time.time - _startTime;

                //跳跃距离
                OnJump(elapse);

                //松开空格后隐藏粒子效果
                Particle.SetActive(false);

                //小人恢复大小
                Body.transform.DOScale(0.18f, 0.8f);
                Head.transform.DOLocalMoveY(0.52f, 0.5f);

                //跳台恢复大小
                _currentStage.transform.DOLocalMoveY(0.25f, 0.2f);
                //_currentStage.transform.DOScale(new Vector3(1, 0.5f, 1), 0.2f);  //这里会导致盒子恢复时出现异常
                _currentStage.transform.DOScaleY(0.5f, 0.2f);

                //_enableInput = false;
            }
            if (Input.GetKey(KeyCode.Space))
            {
                //添加盒子缩放位置的限定
                if (_currentStage.transform.localScale.y > 0.3)
                {
                    //小人缩放
                    Body.transform.localScale += new Vector3(1, -1, 1) * 0.06f * Time.deltaTime;
                    Head.transform.localPosition += new Vector3(0, -1, 0) * 0.06f * Time.deltaTime;

                    //跳台缩放沿着轴心缩放
                    _currentStage.transform.localScale += new Vector3(0, -1, 0) * 0.15f * Time.deltaTime;
                    _currentStage.transform.localPosition += new Vector3(0, -1, 0) * 0.15f * Time.deltaTime;

                }
            }
        }
        
        //是否显示飘分效果
        if(_isUpdateScoreAnimation)
            UpdateScoreAnimation();
       
    }

    //
    //小人跳跃
    //
    void OnJump(float elapse)
    {
        //施加力的方向和大小
        _rigidbody.AddForce((new Vector3(0, 1.25f, 0) + _direction) * elapse * Factor, ForceMode.Impulse);
    }

    //
    //随机确定跳台生成方向
    //
    void RandomDirection()
    {
        var seed = UnityEngine.Random.Range(0, 2);
        if (seed == 0)
        {
            //沿x轴正方向生成
            _direction = new Vector3(1, 0, 0);
        }
        else
        {
            //沿z轴正方向生成
            _direction = new Vector3(0, 0, 1);
        }

    }
    //
    //生成跳台
    //
    void SpawnStage()
    {
        GameObject prefab;
        if (BoxTemplates.Length > 0)
        {
            // 从盒子库中随机取盒子进行动态生成
            prefab = BoxTemplates[Random.Range(0, BoxTemplates.Length)];
        }
        else
        {
            prefab = Stage;
        }

        var stage = Instantiate(prefab);
        stage.transform.position = _currentStage.transform.position + _direction * Random.Range(1.1f, MaxDistance);

        var randomScale = Random.Range(0.5f, 1);
        stage.transform.localScale = new Vector3(randomScale, 0.5f, randomScale);

        // 重载函数 或 重载方法
        stage.GetComponent<Renderer>().material.color =
            new Color(Random.Range(0f, 1), Random.Range(0f, 1), Random.Range(0f, 1));

        ////生成
        ///var stage = Instantiate(Stage);
        ////+随机距离（最小值1.1，最大值）
        //stage.transform.position = _currentStage.transform.position + _direction * UnityEngine.Random.Range(1.1f, MaxDistance);

        ////随机改变跳台大小
        ///var randomScale = UnityEngine.Random.Range(0.5f, 1);
        //stage.transform.localScale = new Vector3(randomScale, 0.5f, randomScale);

        ////改变跳台颜色
        //stage.GetComponent<Renderer>().material.color = new Color(UnityEngine.Random.Range(0.1f, 1), UnityEngine.Random.Range(0.1f, 1), UnityEngine.Random.Range(0.1f, 1));

    }

    //
    //刚体碰撞
    //
    void OnCollisionEnter(Collision collision)
    {
        //碰撞后打印碰撞体名称
        Debug.Log(collision.gameObject.name);
        //判定是否碰到跳台并且不是之前碰撞的跳台
        if (collision.gameObject.name.Contains("Stage") && collision.collider != _lastCollisionCollider)
        {
            var contacts = collision.contacts;
            //赋值为当前跳台
            _lastCollisionCollider = collision.collider;
            _currentStage = collision.gameObject;
            AddScore(contacts);
            RandomDirection();
            SpawnStage();
            MoveCamera();
            ShowScoreAnimation();
            //加分
            //_score++;
            //ScoreText.text = _score.ToString();
        }

        if (collision.gameObject.name == "Ground")
        {
            //本局游戏结束，显示上传分数panle
            _enableInput = false;
            ShowSaveScorePanel();
        }
    }

    //
    // 加分，准确度高的分数成倍增加
    //
    private void AddScore(ContactPoint[] contacts)
    {
        if (contacts.Length > 0)
        {
            var hitPoint = contacts[0].point;
            hitPoint.y = 0;

            var stagePos = _currentStage.transform.position;
            stagePos.y = 0;

            var precision = Vector3.Distance(hitPoint, stagePos);
            if (precision < 0.1)
                _lastReward *= 3;
            else if (precision < 0.3 && precision >= 0.1)
                _lastReward *= 2;
            else
                _lastReward = 1;

            _score += _lastReward;
            ScoreText.text = _score.ToString();
            ShowScoreAnimation();
        }
    }

    //
    //展示上传分数界面
    //
    void ShowSaveScorePanel()
    {
        ScoreText.gameObject.SetActive(false);
        TotalScore_S.text = ScoreText.text;
        SaveScorePanel.SetActive(true);
    }

    //
    //显示飘分动画
    //
    private void ShowScoreAnimation()
    {
        _isUpdateScoreAnimation = true;
        _scoreAnimationStartTime = Time.time;
        SingleScoreText.text = "+" + _lastReward;
    }

    //
    //更新飘分动画
    //
    void UpdateScoreAnimation()
    {
        if (Time.time - _scoreAnimationStartTime > 1)
            _isUpdateScoreAnimation = false;

        var playerScreenPos = RectTransformUtility.WorldToScreenPoint(Camera.GetComponent<Camera>(), transform.position);
        SingleScoreText.transform.position = playerScreenPos +
                                             Vector2.Lerp(Vector2.zero, new Vector2(0, 200),
                                             Time.time - _scoreAnimationStartTime);
        SingleScoreText.color = Color.Lerp(Color.black, new Color(0, 0, 0, 0), Time.time - _scoreAnimationStartTime);
    }

    //
    //移动相机
    //
    void MoveCamera()
    {
        //DOTween相机移动 1为时间参数
        Camera.DOMove(transform.position + _cameraRelativePosition, 1);
    }

    //开始面板上的四个按钮 排行榜 登陆 注册 开始游戏
    void OnClickShowListButton()
    {
        StartPanel.SetActive(false);
        ShowRankPanel();
    }
    void OnClickShowLoginPanel()
    {
        LoginPanel.SetActive(true);
    }
    void OnClickShowRegisterPanel()
    {
        RegisterPanel.SetActive(true);
    }
    void OnClickStartButton()
    {
        _enableInput = true;
        StartPanel.SetActive(false);
    }

    //
    //点击保存按钮
    //
    void OnClickSaveButton()
    {
        AVObject gameScore = new AVObject("GameScore");
        gameScore["score"] = _score;
        gameScore["playerName"] = _nowUsername;
        gameScore.SaveAsync().ContinueWith(_ =>
        {
            ShowRankPanel();
        });

        SaveScorePanel.SetActive(false);
    }

    //
    //点击重新开始按钮
    //
    void OnClickRestartButton()
    {
        StartPanel.SetActive(false);
        //本局游戏结束,重新开始
        //重新构建场景 0为Build Settings中场景的值
        SceneManager.LoadScene(0);        
    }

    //
    //注册和登陆界面的返回按钮
    //
    void OnClickReturnButton1()
    {
        LoginPanel.SetActive(false);
    }
    void OnClickReturnButton2()
    {
        RegisterPanel.SetActive(false);
    }

    //
    //登陆按钮
    //
    void OnClickLoginButton()
    {
        
        var userName = Username_L.text;
        var pwd = Password_L.text;
        AVUser.LogInAsync(userName, pwd).ContinueWith(t =>
        {
            if (t.IsFaulted || t.IsCanceled)
            {
                MainThreadDispatcher.Send(_ =>
                {
                    var error = t.Exception.Message; // 登录失败，可以查看错误信息。
                    InfText.text = error;
                    Inf.SetActive(true);
                }, null);
            }
            else
            {
                MainThreadDispatcher.Send(_ =>
                {
                    //登录成功
                    InfText.text = "登陆成功！";
                    Inf.SetActive(true);
                }, null);
            }
        });
        LoginPanel.SetActive(false);
        Username_L.text = "";
        Password_L.text = "";
        _nowUser = AVUser.CurrentUser;
        _nowUsername = _nowUser.Username;

        Users.text = "当前用户：" + _nowUsername;
    }

    //
    //注册按钮
    //
    void OnClickRegisterButton()
    {
        var userName = Username_R.text;
        var pwd = Password_R.text;
        var pwd2 = Password2_R.text;
        var user = new AVUser();
        user.Username = userName;
        user.Password = pwd;

        user.SignUpAsync().ContinueWith(t =>
        {
            var uid = user.ObjectId;
            MainThreadDispatcher.Send(_ =>
            {
                InfText.text = "注册成功！";
                Inf.SetActive(true);
            }, null);
        });
        RegisterPanel.SetActive(false);
        Username_R.text = "";
        Password_R.text = "";
        Password2_R.text = "";
    }

    void OnClickSoundsButton()
    {
        if ( isOnPlay )
        {
            isOnPlay = false;
            _audioSource.Pause();
            SoundsText.text = "音效：关";
        }
        else
        {
            isOnPlay = true;
            _audioSource.Play();
            SoundsText.text = "音效：开";
        }
    }

    //   
    //显示排行榜界面
    //
    void ShowRankPanel()
    {
        AVQuery<AVObject> query = new AVQuery<AVObject>("GameScore").OrderByDescending("score").Limit(10);
        query.FindAsync().ContinueWith(t =>
        {
            var results = t.Result;
            var scores = new List<string>();

            foreach (var result in results)
            {
                var score = result["playerName"] + " : " + result["score"];
                scores.Add(score);
            }

            MainThreadDispatcher.Send(_ =>
            {
               foreach (var score in scores)
                {
                    var item = Instantiate(RankItem);
                    item.GetComponent<Text>().text = score;
                    item.transform.SetParent(RankItem.transform.parent);
                }
                RankPanel.SetActive(true);
            }, null);
        });
    }
}


