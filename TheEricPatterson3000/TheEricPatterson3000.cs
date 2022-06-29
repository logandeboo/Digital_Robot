using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class TheEricPatterson3000 : CogsAgent
{
    // ------------------BASIC MONOBEHAVIOR FUNCTIONS-------------------

    // Initialize values
    protected override void Start()
    {
        base.Start();
        AssignBasicRewards();
    }
    int carryCount = 0;

    // For actual actions in the environment (e.g. movement, shoot laser)
    // that is done continuously
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        LaserControl();
        // Movement based on DirToGo and RotateDir
        if (!IsFrozen())
        {
            if (!IsLaserOn())
            {
                rBody.AddForce(dirToGo * GetMoveSpeed(), ForceMode.VelocityChange);
            }
            transform.Rotate(rotateDir, Time.deltaTime * GetTurnSpeed());
        }
    }



    // --------------------AGENT FUNCTIONS-------------------------

    // Get relevant information from the environment to effectively learn behavior
    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent velocity in x and z axis 
        var localVelocity = transform.InverseTransformDirection(rBody.velocity);
        sensor.AddObservation(localVelocity.x);
        sensor.AddObservation(localVelocity.z);

        // Time remaning
        sensor.AddObservation(timer.GetComponent<Timer>().GetTimeRemaning());

        // Agent's current rotation
        var localRotation = transform.rotation;
        sensor.AddObservation(transform.rotation.y);

        // Agent and home base's position
        sensor.AddObservation(this.transform.localPosition);
        sensor.AddObservation(baseLocation.localPosition);

        // for each target in the environment, add: its position, whether it is being carried,
        // and whether it is in a base
        foreach (GameObject target in targets)
        {
            sensor.AddObservation(target.transform.localPosition);
            sensor.AddObservation(target.GetComponent<Target>().GetCarried());
            sensor.AddObservation(target.GetComponent<Target>().GetInBase());
        }

        // Whether the agent is frozen
        sensor.AddObservation(IsFrozen());
    }

    // For manual override of controls. This function will use keyboard presses to simulate output from your NN 
    public override void Heuristic(float[] actionsOut)
    {
        var discreteActionsOut = actionsOut;
        discreteActionsOut[0] = 0; //Simulated NN output 0
        discreteActionsOut[1] = 0; //....................1
        discreteActionsOut[2] = 0; //....................2
        discreteActionsOut[3] = 0; //....................3

        //TODO-2: Uncomment this next line when implementing GoBackToBase();
        discreteActionsOut[4] = 0;


        if (Input.GetKey(KeyCode.UpArrow))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            discreteActionsOut[0] = 2;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            //TODO-1: Using the above as examples, set the action out for the left arrow press
            discreteActionsOut[1] = 2;
        }


        //Shoot
        if (Input.GetKey(KeyCode.Space))
        {
            discreteActionsOut[2] = 1;
        }

        //GoToNearestTarget
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[3] = 1;
        }


        //TODO-2: implement a keypress (your choice of key) for the output for GoBackToBase();

        if (Input.GetKey(KeyCode.B))
        {
            discreteActionsOut[4] = 1;
        }


    }

    // What to do when an action is received (i.e. when the Brain gives the agent information about possible actions)
    public override void OnActionReceived(float[] act)
    {
        int forwardAxis = (int)act[0]; //NN output 0

        //TODO: Set these variables to their appopriate item from the act list
        int rotateAxis = (int)act[1];
        int shootAxis = (int)act[2];
        int goToTargetAxis = (int)act[3];

        //TODO-2: Uncomment this next line and set it to the appropriate item from the act list
        int goToBaseAxis = (int)act[4];

        //TODO-2: Make sure to remember to add goToBaseAxis when working on that part!
        MovePlayer(forwardAxis, rotateAxis, shootAxis, goToTargetAxis, goToBaseAxis);

    }


    // ----------------------ONTRIGGER AND ONCOLLISION FUNCTIONS------------------------
    // Called when object collides with or trigger (similar to collide but without physics) other objects
    protected override void OnTriggerEnter(Collider collision)
    {



        if (collision.gameObject.CompareTag("HomeBase") && collision.gameObject.GetComponent<HomeBase>().team == GetTeam())
        {
            //Add rewards here
            AddReward(0.0f);

        }
        base.OnTriggerEnter(collision);
    }

    protected override void OnCollisionEnter(Collision collision)
    {


        //target is not in my base and is not being carried and I am not frozen
        if (collision.gameObject.CompareTag("Target") && collision.gameObject.GetComponent<Target>().GetInBase() != GetTeam() && collision.gameObject.GetComponent<Target>().GetCarried() == 0 && !IsFrozen())
        {
            //Add rewards here
            AddReward(.01f);
        }

        if (collision.gameObject.CompareTag("Wall"))
        {
            //Add rewards here
            AddReward(-1.0f);
        }
        if (collision.gameObject.CompareTag("Target"))
        {
            AddReward(5.0f);
        }
        base.OnCollisionEnter(collision);
    }



    //  --------------------------HELPERS---------------------------- 
    private void AssignBasicRewards()
    {
        rewardDict = new Dictionary<string, float>();

        rewardDict.Add("frozen", -1.0f);
        rewardDict.Add("shooting-laser", 0f);
        rewardDict.Add("hit-enemy", 1.0f);
        rewardDict.Add("dropped-one-target", -0.7f);
        rewardDict.Add("dropped-targets", -1.0f);
    }

    private void majorityControlSequence()
    {
        int count = 0;
        foreach (GameObject target in targets)
        {
            if (target.GetComponent<Target>().GetInBase() == team)
            {
                count++;
            }
        }
        if (count >= 5)
        {
            AddReward(1.0f);
            // inittiate offensive sequence 

        }
    }

    private void MovePlayer(int forwardAxis, int rotateAxis, int shootAxis, int goToTargetAxis, int goToBaseAxis)
    //TODO-2: Add goToTargetAxis as an argument to this function ^
    {
        dirToGo = Vector3.zero;
        rotateDir = Vector3.zero;

        Vector3 forward = transform.forward;
        Vector3 backward = -transform.forward;
        Vector3 right = transform.up;
        Vector3 left = -transform.up;



        //fowardAxis: 
        // 0 -> do nothing
        // 1 -> go forward
        // 2 -> go backward
        if (forwardAxis == 0)
        {
            //do nothing. This case is not necessary to include, it's only here to explicitly show what happens in case 0
        }
        else if (forwardAxis == 1)
        {
            dirToGo = forward;
        }
        else if (forwardAxis == 2)
        {
            //TODO-1: Tell your agent to go backward!
            dirToGo = backward;
        }

        //rotateAxis: 
        // 0 -> do nothing
        // 1 -> go right
        // 2 -> go left
        if (rotateAxis == 0)
        {
            //do nothing
        }
        //TODO-1 : Implement the other cases for rotateDir
        if (rotateAxis == 1)
        {
            rotateDir = right;
        }

        if (rotateAxis == 2)
        {
            rotateDir = left;
        }


        //shoot
        if (shootAxis == 1)
        {
            SetLaser(true);
        }
        else
        {
            SetLaser(false);
        }

        //go to the nearest target
        if (goToTargetAxis == 1)
        {
            GoToNearestTarget();
        }

        //TODO-2: Implement the case for goToBaseAxis
        if (goToBaseAxis == 1)
        {
            GoToBase();
        }

        if (carriedTargets.Count == 3)
        {
            AddReward(1.0f);
            GoToBase();
        }





    }

    // Go to home base
    private void GoToBase()
    {
        TurnAndGo(GetYAngle(myBase));
    }

    // Go to the nearest target
    private void GoToNearestTarget()
    {
        GameObject target = GetNearestTarget();
        if (target != null)
        {
            float rotation = GetYAngle(target);
            TurnAndGo(rotation);
        }
    }

    // Rotate and go in specified direction
    private void TurnAndGo(float rotation)
    {

        if (rotation < -5f)
        {
            rotateDir = transform.up;
        }
        else if (rotation > 5f)
        {
            rotateDir = -transform.up;
        }
        else
        {
            dirToGo = transform.forward;
        }
    }

    // return reference to nearest target
    protected GameObject GetNearestTarget()
    {
        float distance = 200;
        GameObject nearestTarget = null;
        foreach (var target in targets)
        {
            float currentDistance = Vector3.Distance(target.transform.localPosition, transform.localPosition);
            if (currentDistance < distance && target.GetComponent<Target>().GetCarried() == 0 && target.GetComponent<Target>().GetInBase() != team)
            {
                distance = currentDistance;
                nearestTarget = target;
            }
        }
        return nearestTarget;
    }

    private float GetYAngle(GameObject target)
    {

        Vector3 targetDir = target.transform.position - transform.position;
        Vector3 forward = transform.forward;

        float angle = Vector3.SignedAngle(targetDir, forward, Vector3.up);
        return angle;

    }
}
