using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpaceshipController : MonoBehaviourPunCallbacks, IPunObservable
{
    [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
    public static GameObject LocalPlayerInstance;

    public bool IsFiring = false;
    private Vector2 rollVal;
    private Vector2 noseVal;
    private float thrustVal;
    private Rigidbody rb;


    public float RollSpeed = 5f;
    public float NoseSpeed = 5f;
    public float EnginePower = 100f;
    public float MaxSpeed;

    [Tooltip("The current Health of our player")]
    public float Health = 1f;

    public Transform Beams;

    public float maxSpeed = 25f;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Awake()
    {
        // #Important
        // used in GameManager.cs: we keep track of the localPlayer instance to prevent instantiation when levels are synchronized
        if (photonView.IsMine)
        {
            SpaceshipController.LocalPlayerInstance = this.gameObject;
        }
        // #Critical
        // we flag as don't destroy on load so that instance survives level synchronization, thus giving a seamless experience when levels load.
        DontDestroyOnLoad(this.gameObject);
    }

    void Update()
    {
        if (!photonView.IsMine)
        {

            return;
        }

        if (photonView.IsMine)
        {
            if (Health <= 0f)
            {
                GameManager.Instance.LeaveRoom();
            }
        }
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine)
        {

            return;
        }

        rb.AddRelativeTorque( Vector3.back * rollVal.x * RollSpeed, ForceMode.Acceleration);
        rb.AddRelativeTorque(Vector3.left * (noseVal.y * -1) * NoseSpeed, ForceMode.Acceleration);

        rb.AddRelativeForce(Vector3.forward * thrustVal * EnginePower, ForceMode.Acceleration);

        //Debug.Log("speed is " + rb.velocity.magnitude);

        //rb.velocity = new Vector3(Mathf.Clamp(rb.velocity.x, -maxSpeed, maxSpeed), Mathf.Clamp(rb.velocity.y, -maxSpeed, maxSpeed), Mathf.Clamp(rb.velocity.z, -maxSpeed, maxSpeed));
    }

    public void OnFire()
    {
        if (!photonView.IsMine)
        {

            return;
        }
        Debug.Log("FIRE!!");
        if (!IsFiring)
        {
            IsFiring = true;
            StartCoroutine(FireBeam());
            
            
        }
        
        
        
    }

    IEnumerator FireBeam()
    {
        
        Beams.transform.localScale = new Vector3(1f, 1f, 100f);

        yield return new WaitForSeconds(2);

        Beams.transform.localScale = new Vector3(1f, 1f, 0f);
        IsFiring = false;
    }

    public void OnRoll(InputValue value)
    {
        if (!photonView.IsMine)
        {
            
            return;
        }
        rollVal = value.Get<Vector2>();
        //Debug.Log("Rolling " + rollVal.ToString());
    }

    public void OnNose(InputValue value)
    {
        if (!photonView.IsMine)
        {

            return;
        }
        noseVal = value.Get<Vector2>();
        //Debug.Log("Rolling " + noseVal.ToString());
    }

    public void OnEnginePower(InputValue value)
    {
        if (!photonView.IsMine)
        {

            return;
        }
        thrustVal = value.Get<float>();
        Debug.Log(photonView.ViewID.ToString() + " is Thrusting");
        //Debug.Log("engine " + thrustVal.ToString());
    }



    /// <summary>
    /// MonoBehaviour method called when the Collider 'other' enters the trigger.
    /// Affect Health of the Player if the collider is a beam
    /// Note: when jumping and firing at the same, you'll find that the player's own beam intersects with itself
    /// One could move the collider further away to prevent this or check if the beam belongs to the player.
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine)
        {
            return;
        }
        // We are only interested in Beamers
        // we should be using tags but for the sake of distribution, let's simply check by name.
        if (!other.name.Contains("Beam"))
        {
            return;
        }
        Health -= 0.1f;
    }
    /// <summary>
    /// MonoBehaviour method called once per frame for every Collider 'other' that is touching the trigger.
    /// We're going to affect health while the beams are touching the player
    /// </summary>
    /// <param name="other">Other.</param>
    void OnTriggerStay(Collider other)
    {
        // we dont' do anything if we are not the local player.
        if (!photonView.IsMine)
        {
            return;
        }
        // We are only interested in Beamers
        // we should be using tags but for the sake of distribution, let's simply check by name.
        if (!other.name.Contains("Beam"))
        {
            return;
        }
        // we slowly affect health when beam is constantly hitting us, so player has to move to prevent death.
        Health -= 0.1f * Time.deltaTime;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // We own this player: send the others our data
            stream.SendNext(IsFiring);
            stream.SendNext(Health);
        }
        else
        {
            // Network player, receive data
            this.IsFiring = (bool)stream.ReceiveNext();
            this.Health = (float)stream.ReceiveNext();
        }
    }
}
