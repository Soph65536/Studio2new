using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PlayerAttack : MonoBehaviour
{
    [Header("Heavy Attack Unlockable")]
    public bool unlockedHeavyAttack = false;
    [SerializeField] private GameObject HammerModel;

    [Header("Damage Numbers")]
    [SerializeField] private int LightDmg = 2;
    [SerializeField] private int HeavyDmg = 5;
    //Adds this amount of damage to the charged heavy attack for every 0.5 secs it is held down
    [SerializeField] private int ChargedHeavyDmgAddition = 5;
    //max damage for charged heavy attack
    [SerializeField] public int MaxChargedHeavyDmg = 25; //public to be referenced by slider

    //for the charged heavy
    [HideInInspector] public int ChargedHeavyDmg = 0; //public to be referenced by slider
    [SerializeField] private float ChargedHeavyTickRate = 0.3f; //charge time increase
    private bool isChargingChargedHeavyAttack = false;

    [Header("Cooldown Delays")]
    public float LightAttackDelay = 0.3f;
    public float HeavyAttackDelay = 2f;

    [Header("Attacks being carried out")]
    public bool lightAttacking = false;
    public bool heavyAttacking = false;

    [Header("Radius for AoE Attacks")]
    [SerializeField] private float heavyAttackRadius = 1.5f;
    [SerializeField] private float chargedHeavyAttackRadius = 2f;

    [Header("Debug")]
    [SerializeField] private bool showLightRadius = false;
    [SerializeField] private bool showHeavyRadius = false;
    [SerializeField] private bool showChargedHeavyRadius = false;

    //animation stuff
    private Animator MyAnim;
    [Header("Player Model")] public GameObject MainCharacter;

    //audio stuff for ability sounds
    [SerializeField] private AudioSource skillAudioSource;

    private PlayerStats stats;

    private enum AttackType
    {
        Light,
        Heavy,
        ChargedHeavy
    }


    private void Start()
    {
        HammerModel.SetActive(false);

        MyAnim = MainCharacter.GetComponent<Animator>();
        stats = GetComponent<PlayerStats>();
    }


    public void OnAttack(InputValue input)
    {
        if (lightAttacking) return;
        StartCoroutine(LightAttack());
    }

    public void OnChargedHeavyAttack(InputValue input)
    {
        if (lightAttacking || 
            heavyAttacking || 
            !unlockedHeavyAttack) return;

        if (input.isPressed) { StartCoroutine("ChargeChargedHeavyAttack"); } //if pressed then start charging
        else { StartCoroutine("ChargedHeavyAttack"); } //if released then stop charging and do attack
    }


    IEnumerator LightAttack()
    {
        if (!stats.isDead)
        {
            lightAttacking = true;
            MyAnim.SetBool("Attacking", lightAttacking);

            AudioManager.Instance.PlayAudio(false, false, skillAudioSource, "Plr_SwordUse");
            DamageEnemy(Physics.OverlapBox(transform.position + MainCharacter.transform.forward, Vector3.one, MainCharacter.transform.rotation), AttackType.Light);

            yield return new WaitForSeconds(LightAttackDelay);

            lightAttacking = false;
            MyAnim.SetBool("Attacking", lightAttacking);
        }
    }

    IEnumerator ChargedHeavyAttack()
    {
        if (!stats.isDead)
        {
            isChargingChargedHeavyAttack = false; //stop charging the attack
            MyAnim.SetBool("ChargingHeavyAttack", isChargingChargedHeavyAttack);

            //sets hammer model to active and other vars to true since heavy attacking
            HammerModel.SetActive(true);
            heavyAttacking = true;
            MyAnim.SetBool("HeavyAttacking", heavyAttacking);

            AudioManager.Instance.PlayAudio(false, false, skillAudioSource, "Plr_HeavyUse");
            //damage enemy based on normal heavy radius or charged heavy radius
            //set charged heavy dmg to heavy dmg if its too low (equivalent to normal heavy attack)
            if (ChargedHeavyDmg < HeavyDmg) 
            { 
                DamageEnemy(Physics.OverlapSphere(transform.position, heavyAttackRadius), AttackType.Heavy);
            }
            else
            {
                DamageEnemy(Physics.OverlapSphere(transform.position, chargedHeavyAttackRadius), AttackType.ChargedHeavy);
            }

            yield return new WaitForSeconds(HeavyAttackDelay);


            ChargedHeavyDmg = 0; //reset charged heavy damage

            //no longer heavy attacking so disable model and other vars
            HammerModel.SetActive(false);
            heavyAttacking = false;
            MyAnim.SetBool("HeavyAttacking", heavyAttacking);
        }
    }


    IEnumerator ChargeChargedHeavyAttack()
    {
        if (!stats.isDead)
        {
            //sets hammer model to active and other vars to true since we are charging heavy attack
            HammerModel.SetActive(true);
            isChargingChargedHeavyAttack = true; //start charging the attack
            MyAnim.SetBool("ChargingHeavyAttack", isChargingChargedHeavyAttack);

            //stores previous player movespeed then stops player from moving
            PlayerMovement playerMovement = GetComponent<PlayerMovement>();
            float MovementWalkSpeed = playerMovement.WalkSpeed;
            playerMovement.WalkSpeed = 0;

            //start playing charging sound
            AudioManager.Instance.PlayAudio(true, true, skillAudioSource, "Plr_ChargeHeavy");

            //increase heavy attack per 0.5 secs that it is being charged
            while (!heavyAttacking && isChargingChargedHeavyAttack && ChargedHeavyDmg < MaxChargedHeavyDmg)
            {
                yield return new WaitForSeconds(ChargedHeavyTickRate);
                ChargedHeavyDmg += ChargedHeavyDmgAddition;
            }

            //stop charging sound because we aren't charging anymore
            AudioManager.Instance.StopAudio(skillAudioSource);

            //reset movespeed to previous player movespeed
            playerMovement.WalkSpeed = MovementWalkSpeed;
        }
    }


    private void DamageEnemy(Collider[] enemiesToAttack, AttackType atkType)
    {
        if (!stats.isDead)
        {
            if (enemiesToAttack.Length > 0)
            {
                foreach (var hitObject in enemiesToAttack)
                {
                    var DamageComp = hitObject.GetComponent<IDamageable>();

                    if (DamageComp != null && DamageComp.GetType() != typeof(PlayerStats))
                    {
                        switch (atkType)
                        {
                            case AttackType.Light:
                                DamageComp.TakeDamage(LightDmg);
                                break;
                            case AttackType.Heavy:
                                DamageComp.TakeDamage(HeavyDmg);
                                hitObject.transform.GetComponent<IShieldObject>()?.BreakShield();
                                break;
                            case AttackType.ChargedHeavy:
                                DamageComp.TakeDamage(ChargedHeavyDmg);
                                hitObject.transform.GetComponent<IShieldObject>()?.BreakShield();
                                break;
                        }
                    }
                }

            }
        }
    }


    private void OnDrawGizmos()
    {
        if (showLightRadius)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position + MainCharacter.transform.forward, MainCharacter.transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }

        if (showHeavyRadius)
        {
            Gizmos.DrawSphere(transform.position, heavyAttackRadius);
        }

        if (showChargedHeavyRadius)
        {
            Gizmos.DrawSphere(transform.position, chargedHeavyAttackRadius);
        }
    }
}

