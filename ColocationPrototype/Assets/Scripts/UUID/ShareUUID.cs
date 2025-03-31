using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class ShareUUID : NetworkBehaviour
{
    public NetworkVariable<FixedString64Bytes> ColocationUUID = new NetworkVariable<FixedString64Bytes>();
}
