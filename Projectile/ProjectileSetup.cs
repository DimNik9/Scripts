
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;


public class ProjectileSetup: MonoBehaviour
{
    public bool activated;

    [Header("Set the basic stats:")]
    [Range(0f,1f)]
    public float bounciness;
    public bool useGravity;

    //Custom gravity
    [Header("[Attribute] - Custom gravity")]
    [Space(7)]
    public bool useCustomGravity;
    public Vector3 gravityDirection;
    public float gravityStrength;
    private PhysicMaterial physic_mat;
    public Rigidbody rb;


    void Start()
    {
        //Setup physics material
        physic_mat = new PhysicMaterial();
        physic_mat.bounciness = bounciness;
        physic_mat.frictionCombine = PhysicMaterialCombine.Minimum;
        physic_mat.bounceCombine = PhysicMaterialCombine.Maximum;
        //Apply the physics material to the collider
        GetComponent<SphereCollider>().material = physic_mat;

        //Don't use unity's gravity, we made our own (to have more control)
        rb.useGravity = useGravity;
    }
 
}
