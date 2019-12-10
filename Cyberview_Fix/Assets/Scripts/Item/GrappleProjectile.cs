﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleProjectile : MonoBehaviour
{
    private Rigidbody2D body2d;
    private float lifetime = 10f; //overwritten by BM_Grapple in Setup
    public float damage = 0;
    private float speed = 50f;
    private bool right = true;
    private int rightFactor;
    public bool projectileDone = false;
    public bool hitEnemyFlag = false;

    public GameObject attachedTerrain;
    public PlayerManager owner;
    private BM_Grapple bm_Grapple;
    public Vector2 playerVel;
    private Vector3 armOneAttach, armTwoAttach;
    public bool grappling = false;
    private bool delayedReleaseCheck;
    private AudioSource grappleFire, grappleHit, grapplePull, jumpSound;

    private void Awake()
    {
        body2d = GetComponent<Rigidbody2D>();
        owner = GameObject.Find("_Player").GetComponent<PlayerManager>();
        bm_Grapple = owner.bm_Grapple;
    }

    // Update is called once per frame
    void Update()
    {
        int xPosFactor;
        if (owner.isFacingRight) { xPosFactor = 1; } else { xPosFactor = -1; }
        armOneAttach = new Vector3(1 * xPosFactor, 0.7f);
        armTwoAttach = new Vector3(3 * xPosFactor, 0.9f);

        LineRenderer lineRenderer = GetComponent<LineRenderer> ();
 
        lineRenderer.SetPosition (0, new Vector3(transform.localPosition.x, transform.localPosition.y, -1));
        Vector3 ownerPos = new Vector3(0,0,0);
        if (bm_Grapple.armSide == ArmSide.ARMONE)
        {
            ownerPos = new Vector3((owner.transform.localPosition + armOneAttach).x, (owner.transform.localPosition + armOneAttach).y, -1);
        } else {
            ownerPos = new Vector3((owner.transform.localPosition + armTwoAttach).x, (owner.transform.localPosition + armTwoAttach).y, -1);
        }
        lineRenderer.SetPosition(1, ownerPos);
        lifetime -= Time.deltaTime;

        if(attachedTerrain == null){            //Hook is flying
            body2d.velocity = new Vector2(speed*rightFactor, 0);
            if(lifetime < 0){
                bm_Grapple.GrappleDead();
                grapplePull.Stop();
                grappleFire.Stop();
                Destroy(gameObject);
            }
            //Debug.Log(lifetime);
        }
        else{                                   //Hook has attached
            body2d.velocity = new Vector2(0,0);
            owner.GetComponent<Rigidbody2D>().velocity = playerVel;

            if(right && owner.transform.position.x > transform.position.x - 3){
                projectileDone = true;
            }
            else if(!right && owner.transform.position.x < transform.position.x + 3){
                projectileDone = true;
            }

            if(projectileDone){
                // Hook is done
                owner.GetComponent<Rigidbody2D>().velocity = new Vector2(0,33);

                bm_Grapple.GrappleDead();
                grapplePull.Stop();
                grappleFire.Stop();
                jumpSound.Stop();
                jumpSound.Play();
                Destroy(gameObject);
            }

            int layerMask = 1 << 8;
            Vector2 direction = transform.localPosition - owner.transform.localPosition;
            float maxDistance = direction.magnitude - 2;
            if (maxDistance < 0) maxDistance = 0;

            Debug.Log("Attached");
            if (Physics2D.Raycast(owner.transform.localPosition, direction, maxDistance, layerMask, -Mathf.Infinity, Mathf.Infinity))
            {
                if (!delayedReleaseCheck) StartCoroutine(DelayedReleaseCheck(direction, maxDistance, layerMask));
            }
        }
    }

    IEnumerator DelayedReleaseCheck(Vector2 direction, float maxDistance, int layerMask)
    {
        delayedReleaseCheck = true;
        yield return new WaitForSeconds(1);
        if (Physics2D.Raycast(owner.transform.localPosition, direction, maxDistance, layerMask, -Mathf.Infinity, Mathf.Infinity))
        {
            Debug.Log("GrappleProjectile -> Path blocked. Detach.");
            owner.GetComponent<Rigidbody2D>().velocity = new Vector2(0, 33);
            bm_Grapple.GrappleDead();
            grapplePull.Stop();
            grappleFire.Stop();
            Destroy(gameObject);
        }
        delayedReleaseCheck = false;
    }

    public void SetupProjectile(float pLifetime, float pDamage, float pSpeed, bool pRight, AudioSource pgrappleHit, AudioSource pgrapplePull, AudioSource pGrappleFire, AudioSource pJumpSound)
    {
        lifetime = pLifetime;
        damage = pDamage;
        speed = pSpeed;
        right = pRight;
        grappleHit = pgrappleHit;
        grapplePull = pgrapplePull;
        grappleFire = pGrappleFire;
        jumpSound = pJumpSound;
        if (!right) { rightFactor = -1; } else { rightFactor = 1; }
        if(!right){
            GetComponent<SpriteRenderer>().flipX = true;
        }
    }

    void OnCollisionEnter2D(Collision2D target)
    {
        if (target.gameObject.layer == 12)
        {
            //do something to the enemy
            var script = target.gameObject.GetComponent<BasicEnemy>();
            if (script != null)
            {
                //script.HitBy(gameObject);
            }
            hitEnemyFlag = true;
        }
        else if(target.gameObject.layer == 8 || target.gameObject.tag == "HeavyBlock" || target.gameObject.tag == "Orb" || target.gameObject.tag == "Boulder")
        {
            //Attach
            attachedTerrain = target.gameObject;
            float angle = Mathf.Atan2(transform.position.y-owner.transform.position.y, transform.position.x-owner.transform.position.x);
            playerVel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            playerVel *= 33;
            grappling = true;
            grappleHit.Stop();
            grappleHit.Play();
            StartCoroutine(PullSoundDelay());
        }
    }

    IEnumerator PullSoundDelay()
    {
        yield return new WaitForSeconds(.1f);
        grappleFire.Stop();
        grapplePull.Stop();
        grapplePull.Play();
    }
}
