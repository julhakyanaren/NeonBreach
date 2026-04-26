using Photon.Pun;
using UnityEngine;

public class PlayerNetworkTransformSync : MonoBehaviourPun, IPunObservable
{
    [Header("Position Sync")]
    [Tooltip("How fast remote position interpolates to the received network position.")]
    [Range(1f, 30f)]
    [SerializeField] private float positionLerpSpeed = 12f;

    [Header("Rotation Sync")]
    [Tooltip("How fast remote yaw interpolates to the received network rotation.")]
    [Range(1f, 30f)]
    [SerializeField] private float rotationLerpSpeed = 12f;

    [Header("Runtime")]
    [Tooltip("Last received network position for remote interpolation.")]
    [SerializeField] private Vector3 networkPosition;

    [Tooltip("Last received network rotation for remote interpolation.")]
    [SerializeField] private Quaternion networkRotation;

    private void Awake()
    {
        networkPosition = transform.position;
        networkRotation = transform.rotation;
    }

    private void Update()
    {
        if (photonView.IsMine)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, networkPosition);

        if (distance > 5f)
        {
            transform.position = networkPosition;
            transform.rotation = networkRotation;
            return;
        }

        float dynamicLerp = positionLerpSpeed;

        if (distance > 1.5f)
        {
            dynamicLerp *= 2.5f;
        }

        transform.position = Vector3.Lerp(
            transform.position,
            networkPosition,
            Time.deltaTime * dynamicLerp);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            networkRotation,
            Time.deltaTime * rotationLerpSpeed);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            return;
        }

        networkPosition = (Vector3)stream.ReceiveNext();
        networkRotation = (Quaternion)stream.ReceiveNext();
    }
}