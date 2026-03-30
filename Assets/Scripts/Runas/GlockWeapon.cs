using UnityEngine;
using System.Collections;

public class GlockWeapon : MonoBehaviour
{
    [Header("--- WEAPON STATS ---")]
    public float damage = 10f;
    public float range = 100f;
    public float fireRate = 0.1f;
    public int currentAmmo = 12;
    public int maxAmmo = 12;
    public float reloadTime = 1.5f;

    [Header("--- VISUAL CONFIG (RECOIL) ---")]
    public float recoilPosStrength = 0.1f; 
    public float recoilRotStrength = 10f;  
    public float returnSpeed = 5f;
    public float snappiness = 10f;

    [Header("--- REFERENCES ---")]
    public Camera fpsCamera;
    public AudioSource audioSource;
    public ParticleSystem muzzleFlash;

    [Header("--- AUDIO ---")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;

    private Vector3 originalPos;
    private Quaternion originalRot;
    
    private Vector3 currentRecoilPos;
    private Vector3 currentRecoilRot;
    private Vector3 targetRecoilPos;
    private Vector3 targetRecoilRot;
    
    private float nextFireTime = 0f;
    private bool isReloading = false;
    private Coroutine flashCoroutine; 

    void Awake()
    {
        originalPos = transform.localPosition;
        originalRot = transform.localRotation;
    }

    void Start()
    {
        currentAmmo = maxAmmo;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (muzzleFlash) muzzleFlash.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        transform.localPosition = originalPos;
        transform.localRotation = originalRot;
        
        targetRecoilPos = Vector3.zero;
        targetRecoilRot = Vector3.zero;
        currentRecoilPos = Vector3.zero;
        currentRecoilRot = Vector3.zero;
        
        isReloading = false;
        
        if (muzzleFlash) muzzleFlash.gameObject.SetActive(false);
    }

    void Update()
    {
        HandleRecoilMath();
        ApplyTransform();

        if (isReloading) return;

        if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
        {
            if (currentAmmo > 0)
            {
                nextFireTime = Time.time + fireRate;
                Shoot();
            }
            else
            {
                PlaySound(emptySound);
            }
        }

        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo)
        {
            StartCoroutine(ReloadRoutine());
        }
    }

    void HandleRecoilMath()
    {
        targetRecoilPos = Vector3.Lerp(targetRecoilPos, Vector3.zero, Time.deltaTime * returnSpeed);
        targetRecoilRot = Vector3.Lerp(targetRecoilRot, Vector3.zero, Time.deltaTime * returnSpeed);
        
        currentRecoilPos = Vector3.Lerp(currentRecoilPos, targetRecoilPos, Time.deltaTime * snappiness);
        currentRecoilRot = Vector3.Lerp(currentRecoilRot, targetRecoilRot, Time.deltaTime * snappiness);
    }

    void ApplyTransform()
    {
        transform.localPosition = originalPos + currentRecoilPos;
        transform.localRotation = Quaternion.Euler(currentRecoilRot) * originalRot;
    }

    void Shoot()
    {
        currentAmmo--;
        ApplyRecoilImpulse();
        PlaySound(shootSound);

        if (muzzleFlash)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            muzzleFlash.gameObject.SetActive(true);
            muzzleFlash.Stop(); 
            muzzleFlash.Play();
            flashCoroutine = StartCoroutine(StopMuzzleFlash());
        }

        RaycastHit hit;
        if (Physics.Raycast(fpsCamera.transform.position, fpsCamera.transform.forward, out hit, range))
        {
            // 1. Empurra o cubo de física
            if (hit.rigidbody) hit.rigidbody.AddForce(-hit.normal * 100f);

            // 2. Avisa O ALVO que ele tomou o tiro
            ReactiveTarget targetScript = hit.collider.GetComponentInParent<ReactiveTarget>();
            if (targetScript != null)
            {
                targetScript.TargetHit(); 
            }
        }
    }

    IEnumerator StopMuzzleFlash()
    {
        yield return new WaitForSeconds(0.1f);
        if (muzzleFlash) muzzleFlash.gameObject.SetActive(false);
        flashCoroutine = null;
    }

    void ApplyRecoilImpulse()
    {
        targetRecoilPos += Vector3.back * recoilPosStrength;
        targetRecoilRot += new Vector3(-recoilRotStrength, Random.Range(-5f, 5f), 0);
    }

    IEnumerator ReloadRoutine()
    {
        isReloading = true;
        PlaySound(reloadSound);
        
        if (muzzleFlash) muzzleFlash.gameObject.SetActive(false);

        targetRecoilRot += new Vector3(45f, 0, 0); 
        targetRecoilPos += new Vector3(0, -0.2f, 0);
        
        yield return new WaitForSeconds(reloadTime);
        
        currentAmmo = maxAmmo;
        isReloading = false;
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource && clip) audioSource.PlayOneShot(clip);
    }
}