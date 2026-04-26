using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class EnemyNetworkTransformSync : MonoBehaviourPun, IPunObservable
{
    [System.Serializable]
    private struct NetworkState
    {
        public double time;
        public Vector3 position;
        public Quaternion rotation;
    }

    [Header("Interpolation")]
    [Tooltip("How far behind network time remote enemies are rendered.")]
    [Range(0.05f, 0.3f)]
    [SerializeField] private float interpolationBackTime = 0.12f;

    [Tooltip("If distance is bigger than this value, remote enemy teleports to network position.")]
    [SerializeField] private float teleportDistance = 15f;

    [Tooltip("Maximum stored network states.")]
    [Range(2, 30)]
    [SerializeField] private int bufferSize = 20;

    [Header("Runtime Debug")]
    [Tooltip("How many states are currently stored.")]
    [SerializeField] private int bufferedStatesCount;

    private readonly List<NetworkState> states = new List<NetworkState>();

    private void LateUpdate()
    {
        if (CanWriteTransform())
        {
            return;
        }

        ApplyBufferedTransform();
    }

    private bool CanWriteTransform()
    {
        if (!RuntimeOptions.MultiplayerMode)
        {
            return true;
        }

        if (!PhotonNetwork.InRoom)
        {
            return true;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            return true;
        }

        return false;
    }

    private void ApplyBufferedTransform()
    {
        bufferedStatesCount = states.Count;

        if (states.Count == 0)
        {
            return;
        }

        double renderTime = PhotonNetwork.Time - interpolationBackTime;

        if (states.Count == 1)
        {
            ApplyState(states[0]);
            return;
        }

        for (int i = 0; i < states.Count - 1; i++)
        {
            NetworkState olderState = states[i];
            NetworkState newerState = states[i + 1];

            if (olderState.time <= renderTime && renderTime <= newerState.time)
            {
                float length = (float)(newerState.time - olderState.time);
                float t = 0f;

                if (length > 0.0001f)
                {
                    t = (float)((renderTime - olderState.time) / length);
                }

                Vector3 interpolatedPosition = Vector3.Lerp(
                    olderState.position,
                    newerState.position,
                    t);

                Quaternion interpolatedRotation = Quaternion.Slerp(
                    olderState.rotation,
                    newerState.rotation,
                    t);

                ApplyPositionAndRotation(interpolatedPosition, interpolatedRotation);
                RemoveOldStates(i);
                return;
            }
        }

        NetworkState lastState = states[states.Count - 1];
        ApplyPositionAndRotation(lastState.position, lastState.rotation);
    }

    private void ApplyState(NetworkState state)
    {
        ApplyPositionAndRotation(state.position, state.rotation);
    }

    private void ApplyPositionAndRotation(Vector3 targetPosition, Quaternion targetRotation)
    {
        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance > teleportDistance)
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            return;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }

    private void RemoveOldStates(int lastUsedOlderIndex)
    {
        if (lastUsedOlderIndex <= 0)
        {
            return;
        }

        states.RemoveRange(0, lastUsedOlderIndex);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            return;
        }

        NetworkState state = new NetworkState();
        state.time = info.SentServerTime;
        state.position = (Vector3)stream.ReceiveNext();
        state.rotation = (Quaternion)stream.ReceiveNext();

        states.Add(state);

        states.Sort((a, b) => a.time.CompareTo(b.time));

        while (states.Count > bufferSize)
        {
            states.RemoveAt(0);
        }
    }
}