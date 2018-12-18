using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;//场景
using UnityEngine.UI;//分数
using DG.Tweening;//相机跟随插件

public class Player : MonoBehaviour
{
    //跳跃因数 每秒跳跃距离
    public float Factor=4;

    //生成跳台的最大距离
    public float MaxDistance=3;
    //获取第一个跳台
    public GameObject Stage;

    //蓄力效果
    public GameObject Particle;

    //小人头部 身体（蓄力效果）
    public Transform Head;
    public Transform Body;

    //获取相机位置
    public Transform Camera;

    //计分板
    public Text ScoreText;

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

    private int _score;

    //初始化沿x轴正方向生成跳台
    Vector3 _direction = new Vector3(1, 0, 0);


    // Start is called before the first frame update
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
    }

    // Update is called once per frame
    void Update()
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
            Body.transform.DOScale(0.1f, 1);
            Head.transform.DOLocalMoveY(0.29f, 0.5f);

            //跳台恢复大小
            _currentStage.transform.DOLocalMoveY(0.25f, 0.2f);
            _currentStage.transform.DOScale(new Vector3(1, 0.5f, 1), 0.2f);
        }
        if (Input.GetKey(KeyCode.Space))
        {
            //小人缩放
            Body.transform.localScale += new Vector3(1, -1, 1) * 0.05f * Time.deltaTime;
            Head.transform.localPosition += new Vector3(0, -1, 0) * 0.1f * Time.deltaTime;

            //跳台缩放沿着轴心缩放
            _currentStage.transform.localScale += new Vector3(0, -1, 0) * 0.15f * Time.deltaTime;
            _currentStage.transform.localPosition += new Vector3(0, -1, 0) * 0.15f * Time.deltaTime;
        }
    }

    //小人跳跃
    void OnJump(float elapse)
    {
        //施加力的方向和大小
        _rigidbody.AddForce((new Vector3(0, 1, 0) + _direction) * elapse * Factor, ForceMode.Impulse);
    }

    //随机确定跳台生成方向
    void RandomDirection()
    {
        var seed = Random.Range(0, 2);
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

    //生成跳台
    void SpawnStage()
    {
        //生成
        var stage = Instantiate(Stage);
        //+随机距离（最小值1.1，最大值）
        stage.transform.position = _currentStage.transform.position + _direction * Random.Range(1.1f, MaxDistance);

        //随机改变跳台大小
        var randomScale = Random.Range(0.5f, 1);
        Stage.transform.localScale = new Vector3(randomScale, 0.5f, randomScale);

        //改变跳台颜色
        stage.GetComponent<Renderer>().material.color = new Color(Random.Range(0.1f, 1), Random.Range(0.1f, 1), Random.Range(0.1f, 1));

    }

    //刚体碰撞
    void OnCollisionEnter(Collision collision)
    {
        //碰撞后打印碰撞体名称
        Debug.Log(collision.gameObject.name);
        //判定是否碰到跳台并且不是之前碰撞的跳台
        if (collision.gameObject.name.Contains("Stage")&& collision.collider !=_lastCollisionCollider)
        {
            //赋值为当前跳台
            _lastCollisionCollider = collision.collider;
            _currentStage = collision.gameObject;
            RandomDirection();
            SpawnStage();
            MoveCamera();

            //加分
            _score++;
            ScoreText.text = "分数："+_score.ToString();
        }

        if(collision.gameObject.name=="Ground")
        {
            //本局游戏结束,重新开始
            //重新构建场景 0为Build Settings中场景的值
            SceneManager.LoadScene(0);
        }
    }

    //移动相机
    void MoveCamera()
    {
        //DOTween相机移动 1为时间参数
        Camera.DOMove(transform.position + _cameraRelativePosition, 1);
    }
}
