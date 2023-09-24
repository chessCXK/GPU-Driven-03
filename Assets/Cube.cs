using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;

public class Cube : MonoBehaviour
{

    private float time;

    public EasyTouchMove stick;
    public EasyTouchMove stickRotation;

    public float speed = 5f;

    //旋转最大角度
    public int yRotationMinLimit = -20;
    public int yRotationMaxLimit = 80;
    //旋转速度
    public float xRotationSpeed = 250.0f;
    public float yRotationSpeed = 120.0f;
    //旋转角度
    private float xRotation = 0.0f;
    private float yRotation = 0.0f;

    // Use this for initialization
    void Start()
    {

    }

    float ClampValue(float value, float min, float max)//控制旋转的角度
    {
        if (value < -360)
            value += 360;
        if (value > 360)
            value -= 360;
        return Mathf.Clamp(value, min, max);//限制value的值在min和max之间， 如果value小于min，返回min。 如果value大于max，返回max，否则返回value
    }
           // Update is called once per frame
    void Update()
    {

        Vector2 stickValue;
        stickValue = stickRotation.TouchedAxis;
        //Input.GetAxis("MouseX")获取鼠标移动的X轴的距离
        xRotation -= stickValue.x * xRotationSpeed * 0.02f;
        yRotation += stickValue.y * yRotationSpeed * 0.02f;

        yRotation = ClampValue(yRotation, yRotationMinLimit, yRotationMaxLimit);//这个函数在结尾
                                                                                //欧拉角转化为四元数
        Quaternion rotation = Quaternion.Euler(-yRotation, -xRotation, 0);
        if (stickValue.x != 0 || stickValue.y != 0)
        {
            transform.rotation = rotation;
        }
        
        //if (autoMove)
        //    stickValue = autoMoveDir;
        //else
        stickValue = stick.TouchedAxis;

        if (stickValue.x == 0 && stickValue.y == 0)
        {
            time = 0;
            // anim.SetBool("run", false);
            return;
        }

        transform.position += stickValue.y * transform.forward * speed * 0.01f;
        transform.position += stickValue.x * transform.right * speed * 0.01f;

       

    }
}
