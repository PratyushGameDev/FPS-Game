using System.Collections;
using UnityEngine;
using Photon.Pun;
using System.Linq;
using TMPro;

public class SingleShotGun : Gun
{

    [SerializeField] Camera cam;

    public bool canDaggerSwing = true;

    bool isSniperScoped = false;

    public GameObject bulletImpactVFX;
    public GameObject[] scopePieces;

    PhotonView PV;

    public int playerID;

    [Header("Gun Settings")]

    public float fireRate = 0.1f;
    public int clipSize = 30;
    public int reservedAmmoCapacity = 270;
    public bool isAutomatic;
    public float reloadTime;

    //Variables that change throughout the code
    public bool _canShoot;
    int _currentAmmoInClip;
    int _ammoInReserve;

    //Muzzle Flash
    public GameObject muzzleFlashEffect;
    public Transform muzzleFlashSpawnPlace;

    //Aiming
    public Vector3 normalLocalPosition;
    public Vector3 aimingLocalPosition;
    public float aimSmoothing = 10;

    //Weapon Sway
    public float weaponSwayAmount = 10;

    //Weapon Recoil
    public bool randomizeRecoil;
    public Vector2 randomRecoilContstraints;
    //you only need to assign this is randomizerecoil is off
    public Vector2 recoilPattern;
    public float recoilAmount = 0.1f;

    bool isAiming;

    [Header("Animations")]
    //Reloading
    public Animator animator;
    public string reload;
    public bool doesHaveAnimationForShooting;
    public string shoot;
    bool isReloading = false;
    public AudioSource audioSource;
    public AudioClip reloadSFX;
    public AudioClip reloadSFX4;
    public AudioClip reloadSFX3;
    public AudioClip reloadSFX2;
    public AudioClip reloadSFX1;
    public AudioClip outOfAmmoSFX;
    public bool isPistol;
    public string pistolOutOfAmmo;

    public PlayerController playerController;

    public bool isSniper;
    bool isScoped = false;
    public bool isShotGun;
    //public GameObject scope;

    public Vector2 _currentRotation;
    public bool canAim = true;

    public float shotGunRange;

    //Gun UI
    public TMP_Text bulletCount;

    public AudioClip[] shootSFX;

    public AudioClip equip;

    public bool isDagger;

    public AudioClip scopeSFX;

    public GameObject otherGunScopeReference;

    public float bulletBloomAmount;
    public float doubleBulletBloom;
    public float originalBloom;

    [SerializeField] GameObject shield;

    public GameObject[] weapons;

    public KeyCode inputKey;

    [SerializeField] GameObject crosshairCanvas;

    public TrailRenderer trail;

    public Shader sniperScope;
    public GameObject sniperGlassScope;

    float weaponRecoilOrigional;

    private void Start()
    {
        _currentAmmoInClip = clipSize;
        _ammoInReserve = reservedAmmoCapacity;
        _canShoot = true;
        if (PV.IsMine)
        {
            if (FirebaseManager.Singleton.crosshairs == false)
            {
                crosshairCanvas.SetActive(false);
            }
            if (isSniper)
            {
                Material sniperScopeMat = new(sniperScope);
                sniperGlassScope.GetComponent<MeshRenderer>().material = sniperScopeMat;
            }
        }
    }

    private void Awake()
    {
        PV = GetComponent<PhotonView>();
        doubleBulletBloom = bulletBloomAmount * 2;
        originalBloom = bulletBloomAmount;
        weaponRecoilOrigional = recoilAmount;
    }

    private void FixedUpdate()
    {
        if (PV.IsMine == false)
            return;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))
        {
            bulletBloomAmount = doubleBulletBloom;
        }
        if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.D))
        {
            bulletBloomAmount = originalBloom;
        }

        if (isReloading)
        {
            if (Input.GetAxis("Mouse ScrollWheel") < 0 || Input.GetAxis("Mouse ScrollWheel") > 0)
            {
                Debug.Log("Mouse scroll");
                foreach (GameObject gun in weapons)
                {
                    gun.GetComponent<SingleShotGun>().StopCoroutine("Reload");
                    audioSource.Stop();
                    canAim = true;
                    isReloading = false;
                    _canShoot = true;
                }
            }

            if (Input.anyKeyDown)
            {
                KeyCode[] possibleGuns = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Q };

                foreach (KeyCode key in possibleGuns)
                {
                    if (Input.GetKeyDown(key))
                    {
                        if (key != inputKey)
                        {
                            foreach (GameObject gun in weapons)
                            {
                                gun.GetComponent<SingleShotGun>().StopCoroutine("Reload");
                                audioSource.Stop();
                                canAim = true;
                                isReloading = false;
                                _canShoot = true;
                            }
                        }
                    }
                }
            }
        }

    }

    private void CamWarp()
    {
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            if (isDagger)
                return;

            if (!isSniper)
            {
                cam.GetComponent<Animator>().Play("CameraZoomIn");
            }
            else
            {
                cam.GetComponent<Animator>().Play("SniperCameraZoomIn");
                Debug.Log("is a sniper");
            }

        }
        if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            if (isDagger)
                return;

            if (!isSniper)
            {
                cam.GetComponent<Animator>().Play("CameraZoomOut");
            }
            else
            {
                cam.GetComponent<Animator>().Play("SniperCameraZoomOut");
            }
        }
    }

    public void DisableGun()
    {
        playerController.canSwitchWeapons = false;
        transform.GetChild(0).gameObject.SetActive(false);
        canAim = false;
        _canShoot = false;
        this.gameObject.GetComponent<SingleShotGun>().enabled = false;
    }

    public override void Use()
    {
        if (PV.IsMine == false)
            return;
            

        Shoot();
        CamWarp();

        if (!isSniper)
        {
            GameObject sniper = GameObject.Find("Sniper");
            otherGunScopeReference.SetActive(false);
            isScoped = false;
            cam.fieldOfView = 60;
            playerController.mouseSensitivity = 3;
            for (int i = 0; i < sniper.GetComponent<SingleShotGun>().scopePieces.Count(); i++)
            {
                sniper.GetComponent<SingleShotGun>().scopePieces[i].SetActive(true);
            }
        }
        else
        {
            Vector2 screenPixels = cam.WorldToScreenPoint(transform.position);
            screenPixels = new Vector2(screenPixels.x / Screen.width, screenPixels.y / Screen.height);
            Debug.Log("Should be glassing");
            sniperGlassScope.GetComponent<MeshRenderer>().material.SetVector("_ObjectScreenPosition", screenPixels);

        }
    }
    [PunRPC]
    void PlayShootSFX()
    {
        int clipToPlay = Random.Range(0, shootSFX.Length - 1);
        audioSource.PlayOneShot(shootSFX[clipToPlay]);
    }

    void Shoot()
    {
        if (isDagger == false)
        {
            DetermineAim();
        }

        if (!isReloading) 
        {
            if (!isSniperScoped)
               {
                    if (!Input.GetKey(KeyCode.Mouse1)) 
                    {
                           DetermineWeaponSway();

                    }

              } 
        }
               

        if (isDagger)
        {
            bulletCount.text = "";
        } else if (!isDagger)
        {
            bulletCount.text = _currentAmmoInClip + " / " + clipSize;
        }     

        if (isAutomatic)
        {
            if (Input.GetMouseButton(0) && _canShoot && _currentAmmoInClip > 0)
            {
                PV.RPC(nameof(PlayShootSFX), RpcTarget.All);
                _canShoot = false;
                playerController.canSwitchWeapons = false;
                _currentAmmoInClip--;


                StartCoroutine(ShootGun());
                Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
                ray.origin = new Vector3((cam.transform.position.x + Random.Range(-bulletBloomAmount, bulletBloomAmount)), (cam.transform.position.y + Random.Range(-bulletBloomAmount, bulletBloomAmount)), (cam.transform.position.z + Random.Range(0, 0)));
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider.gameObject != transform.root.gameObject)
                    {
                        if (!hit.collider.gameObject.CompareTag("Player"))
                        {
                            hit.collider.gameObject.transform.root.gameObject.GetComponent<IDamageable>()?.TakeDamage(((GunInfo)itemInfo).damage);
                        }
                        if (hit.collider.transform.name == "glass (1)")
                        {
                            if (hit.collider.transform.parent.transform.parent.GetComponent<ShieldManager>().hasOpenedShield)
                            {
                                hit.collider.transform.parent.gameObject.GetComponent<ShieldManager>().TakeHit();
                            }
                        }                        
                    }                    
                    PV.RPC("RPC_Shoot", RpcTarget.All, hit.point, hit.normal);
                }
                if (doesHaveAnimationForShooting)
                {
                    if (isAiming)
                        return;
                    animator.Play(shoot.ToString(), 0, 0.0f);
                }
            }
            else if (Input.GetKeyDown(KeyCode.R) && _currentAmmoInClip < clipSize && _ammoInReserve > 0 && !isAiming)
            {
                if (!_canShoot || isReloading)
                    return;

                StartCoroutine("Reload");
                if (isShotGun == false)
                {
                    audioSource.Stop();
                    audioSource.PlayOneShot(reloadSFX);
                }
                else if (isShotGun)
                {
                    audioSource.Stop();
                    if (_currentAmmoInClip == 4)
                    {
                        audioSource.PlayOneShot(reloadSFX1);
                    }
                    if (_currentAmmoInClip == 3)
                    {
                        audioSource.PlayOneShot(reloadSFX2);
                    }
                    if (_currentAmmoInClip == 2)
                    {
                        audioSource.PlayOneShot(reloadSFX3);
                    }
                    if (_currentAmmoInClip == 1)
                    {
                        audioSource.PlayOneShot(reloadSFX4);
                    }
                    if (_currentAmmoInClip == 0)
                    {
                        audioSource.PlayOneShot(reloadSFX);
                    }
                }
            }
        } else if (!isAutomatic)
        {
            if (Input.GetMouseButtonDown(0) && _canShoot && _currentAmmoInClip > 0)
            {
                if (isDagger == false)
                {
                    _currentAmmoInClip--;
                }
                PV.RPC(nameof(PlayShootSFX), RpcTarget.All);
                _canShoot = false;
                playerController.canSwitchWeapons = false;
                StartCoroutine(ShootGun());
                if (isShotGun == false && isDagger == false)
                {
                    Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
                    ray.origin = new Vector3((cam.transform.position.x + Random.Range(-bulletBloomAmount, bulletBloomAmount)), (cam.transform.position.y + Random.Range(-bulletBloomAmount, bulletBloomAmount)), (cam.transform.position.z + Random.Range(0, 0)));

                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        if (hit.collider.gameObject != transform.root.gameObject)
                        {
                            if (!hit.collider.gameObject.CompareTag("Player"))
                            {
                                hit.collider.gameObject.transform.root.gameObject.GetComponent<IDamageable>()?.TakeDamage(((GunInfo)itemInfo).damage);
                            }
                            if (hit.collider.transform.name == "glass (1)")
                            {
                                if (hit.collider.transform.parent.transform.parent.GetComponent<ShieldManager>().hasOpenedShield)
                                {
                                    hit.collider.transform.parent.gameObject.GetComponent<ShieldManager>().TakeHit();
                                }
                            }
                        }                        
                        PV.RPC("RPC_Shoot", RpcTarget.All, hit.point, hit.normal);
                    }
                } else if (isShotGun)
                {
                    float x = Random.Range(-0.1f, 0.1f);
                    float y = Random.Range(-0.1f, 0.1f);
                    float x1 = Random.Range(-0.1f, 0.1f);
                    float y1 = Random.Range(-0.1f, 0.1f);
                    float x2 = Random.Range(-0.1f, 0.1f);
                    float y2 = Random.Range(-0.1f, 0.1f);
                    float x3 = Random.Range(-0.1f, 0.1f);
                    float y3 = Random.Range(-0.1f, 0.1f);
                    float x4 = Random.Range(-0.1f, 0.1f);
                    float y4 = Random.Range(-0.1f, 0.1f);
                    float x5 = Random.Range(-0.1f, 0.1f);
                    float y5 = Random.Range(-0.1f, 0.1f);
                    float x6 = Random.Range(-0.1f, 0.1f);
                    float y6 = Random.Range(-0.1f, 0.1f);
                    float x7 = Random.Range(-0.1f, 0.1f);
                    float y7 = Random.Range(-0.1f, 0.1f);

                    Vector3 directionOfRay = cam.transform.forward + new Vector3(x, y, 0);
                    Vector3 directionOfRay1 = cam.transform.forward + new Vector3(x1, y1, 0);
                    Vector3 directionOfRay2 = cam.transform.forward + new Vector3(x2, y2, 0);
                    Vector3 directionOfRay3 = cam.transform.forward + new Vector3(x3, y3, 0);
                    Vector3 directionOfRay4 = cam.transform.forward + new Vector3(x4, y4, 0);
                    Vector3 directionOfRay5 = cam.transform.forward + new Vector3(x5, y5, 0);
                    Vector3 directionOfRay6 = cam.transform.forward + new Vector3(x6, y6, 0);
                    Vector3 directionOfRay7 = cam.transform.forward + new Vector3(x7, y7, 0);

                    Vector3[] directions = { directionOfRay, directionOfRay1, directionOfRay2, directionOfRay3, directionOfRay4, directionOfRay5, directionOfRay6, directionOfRay7 };

                    foreach (Vector3 dir in directions)
                    {
                        if (Physics.Raycast(cam.transform.position, dir, out RaycastHit hit, shotGunRange))
                        {
                            if (hit.collider.gameObject != transform.root.gameObject)
                            {
                                if (!hit.collider.gameObject.CompareTag("Player"))
                                {
                                    hit.collider.gameObject.transform.root.gameObject.GetComponent<IDamageable>()?.TakeDamage(((GunInfo)itemInfo).damage);
                                }
                                if (hit.collider.transform.name == "glass (1)")
                                {
                                    if (hit.collider.transform.parent.transform.parent.GetComponent<ShieldManager>().hasOpenedShield)
                                    {
                                        hit.collider.transform.parent.gameObject.GetComponent<ShieldManager>().TakeHit();
                                    }
                                }
                            }
                            PV.RPC("RPC_Shoot", RpcTarget.All, hit.point, hit.normal);
                        }
                    }                    
                }                
                if (doesHaveAnimationForShooting)
                {
                    if (isAiming)
                        return;
                    if (isDagger)
                    {
                        int num = Random.Range(1, 3);
                        animator.Play("KnifeSwing" + num);
                    }else
                    {
                        animator.Play(shoot.ToString(), 0, 0.0f);

                    }
                }
            }            
            else if (Input.GetKeyDown(KeyCode.R) && _currentAmmoInClip < clipSize && _ammoInReserve > 0 && !isDagger && !isAiming)
            {
                if (!_canShoot || isReloading)
                    return;

                StartCoroutine("Reload");

                if (isShotGun == false)
                {
                    audioSource.Stop();
                    audioSource.PlayOneShot(reloadSFX);
                }
                else if (isShotGun)
                {
                    audioSource.Stop();
                    if (_currentAmmoInClip == 4)
                    {
                        audioSource.PlayOneShot(reloadSFX1);
                    }
                    if (_currentAmmoInClip == 3)
                    {
                        audioSource.PlayOneShot(reloadSFX2);
                    }
                    if (_currentAmmoInClip == 2)
                    {
                        audioSource.PlayOneShot(reloadSFX3);
                    }
                    if (_currentAmmoInClip == 1)
                    {
                        audioSource.PlayOneShot(reloadSFX4);
                    }
                    if (_currentAmmoInClip == 0)
                    {
                        audioSource.PlayOneShot(reloadSFX);
                    }
                }
            }
        }

        if (Input.GetMouseButtonDown(0) && _canShoot)
        {
            if (!isSniper)
            {
                if (_currentAmmoInClip <= 0)
                {
                    if (isPistol)
                    {
                        animator.Play(pistolOutOfAmmo);
                    }

                    audioSource.Stop();
                    audioSource.PlayOneShot(outOfAmmoSFX);
                }
            } else if (isSniper)
            {
                if (_currentAmmoInClip <0)
                {
                    audioSource.Stop();
                    audioSource.PlayOneShot(outOfAmmoSFX);
                }
            }
            
        }
        
    }

    IEnumerator Reload()
    {
        isReloading = true;

        if (isShotGun == false)
        {
            animator.Play(reload.ToString(), 0, 0.0f);
        }
        else if (isShotGun)
        {
            if (_currentAmmoInClip == 4)
            {
                animator.Play("BenelliReload1", 0, 0.0f);
                reloadTime = 46f;
            }
            if (_currentAmmoInClip == 3)
            {
                animator.Play("BenelliReload2", 0, 0.0f);
                reloadTime = 60f;
            }
            if (_currentAmmoInClip == 2)
            {
                animator.Play("BenelliReload3", 0, 0.0f);
                reloadTime = 66f;
            }
            if (_currentAmmoInClip == 1)
            {
                animator.Play("BenelliReload4", 0, 0.0f);
                reloadTime = 80f;
            }
            if (_currentAmmoInClip == 0)
            {
                reloadTime = 87f;
                animator.Play("BenelliReload5", 0, 0.0f);
            }
        }

        if (isSniper)
        {
            //scope.SetActive(false);
            isScoped = false;
            cam.fieldOfView = 60;
            playerController.mouseSensitivity = 3;
            for (int i = 0; i < scopePieces.Count(); i++)
            {
                scopePieces[i].SetActive(true);
            }
        }
        canAim = false;
        _canShoot = false;

        yield return new WaitForSecondsRealtime(reloadTime * Time.smoothDeltaTime);

        canAim = true;
        isReloading = false;
        _canShoot = true;
        playerController.canSwitchWeapons = true;

        _currentAmmoInClip = clipSize;
    }

    void DetermineWeaponSway()
    {
        if (isDagger)
            return;

        Vector2 mouseAxis = new(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        //float mouseAxis = Input.GetAxisRaw("Mouse X");

        transform.localPosition += (Vector3)mouseAxis * weaponSwayAmount / 1000;
        //transform.localPosition += new Vector3 (mouseAxis * weaponSwayAmount / 1000, 0, 0);
    }

    void DetermineAim()
    {
        if (canAim == false)
            return;

        Vector3 target = normalLocalPosition;
        if (Input.GetMouseButton(1))
        {            
            if (isSniper)
            {
                isSniperScoped = true;
                sniperGlassScope.GetComponent<MeshRenderer>().material.SetFloat("_ZoomAmount", .4f);
            }
            recoilAmount = recoilAmount / 3;
            target = aimingLocalPosition;
            isAiming = true;
        }
        else if (Input.GetMouseButtonUp(1) && isSniper)
        {
            if (isSniper)
            {
                isSniperScoped = false;
                sniperGlassScope.GetComponent<MeshRenderer>().material.SetFloat("_ZoomAmount", .2f);

            }
        }   
        
        if (Input.GetMouseButtonDown(1))
        {
            bulletBloomAmount = doubleBulletBloom;
            if (isDagger)
                return;
            playerController.walkSpeed = 2.5f;
            playerController.sprintSpeed = 5;

        }
        else if (Input.GetMouseButtonUp(1))
        {
            bulletBloomAmount = originalBloom;
            if (isDagger)
                return;
            playerController.walkSpeed = 5;
            playerController.sprintSpeed = 10;
            isAiming = false;
            recoilAmount = weaponRecoilOrigional;
        }


        if (isSniper)
        {

            Debug.Log(transform.gameObject.name);
            Debug.Log(transform.localPosition);
            if (Input.GetMouseButtonDown(1))
            {
                Debug.Log("Should enable scope NOW");
                //StartCoroutine(nameof(OpenScope));
                isScoped = true;
                audioSource.Stop();
                audioSource.PlayOneShot(scopeSFX);
                cam.fieldOfView = 30;

                playerController.mouseSensitivity = 0.5f;

            }

            else if (Input.GetMouseButtonUp(1))
            {
                //StopCoroutine(nameof(OpenScope));
                isScoped = false;
                cam.fieldOfView = 60;
                playerController.mouseSensitivity = 3;
                for (int i = 0; i < scopePieces.Count(); i++)
                {
                    scopePieces[i].SetActive(true);
                }
                //scope.SetActive(false);
            }
        }       

        Vector3 desiredPosition = Vector3.Lerp(transform.localPosition, target, Time.deltaTime * aimSmoothing);

        transform.localPosition = desiredPosition;
    }

    //IEnumerator OpenScope()
    //{
       
    //    yield return new WaitForSeconds(Time.deltaTime * aimSmoothing);
    //    Debug.Log(Input.GetMouseButton(1));
    //    if (Input.GetMouseButton(1))
    //    {
    //        //scope.SetActive(true);
    //        for (int i = 0; i < scopePieces.Count(); i++)
    //        {
    //            scopePieces[i].SetActive(false);
    //        }
    //    }            
    //    //weaponCam.SetActive(false);
    //    //Disable sniper visuals
    //    //Modify muzzle flash
    //}
    IEnumerator ShootGun()
    {
        if (isDagger == false)
        {
            GameObject flash = Instantiate(muzzleFlashEffect, muzzleFlashSpawnPlace);
            //flash.GetComponent<ParticleSystem>().Emit(1);
            //flash.transform.Find("Sparks").GetComponent<ParticleSystem>().Emit(1);
            Destroy(flash, 1f);
            if (isShotGun)
            {
                cam.GetComponentInParent<Shake>().duration = 0.4f;
            }
            if (isSniper)
            {
                cam.GetComponentInParent<Shake>().duration = 0.3f;
            }
            if (!isShotGun || !isSniper)
            {
                cam.GetComponentInParent<Shake>().duration = 0.1f;
            }
            //cam.GetComponentInParent<Shake>().start = true;
        }
        if (isDagger)
        {
            canDaggerSwing = false;
        }
        DetermineRecoil();


        yield return new WaitForSecondsRealtime(fireRate * Time.smoothDeltaTime);

        _canShoot = true;
        canDaggerSwing = true;
        playerController.canSwitchWeapons = true;
    }

    void DetermineRecoil()
    {
        if (!isDagger) 
        {
            transform.localPosition -= Vector3.forward * recoilAmount;       
        }
    }

    [PunRPC]
    void RPC_Shoot(Vector3 hitPosition, Vector3 hitNormal)
    {
        if (!PhotonNetwork.InRoom)
            return;

        if (!PV.IsMine)
            return;

        TrailRenderer _trail = Instantiate(trail, muzzleFlashSpawnPlace.position, Quaternion.identity);

        StartCoroutine(SpawnTrail(_trail, hitPosition));
            
        Collider[] colliders = Physics.OverlapSphere(hitPosition, 0.3f);
        if (colliders.Length != 0)
        {
            GameObject impactObject = Instantiate(bulletImpactVFX, hitPosition, Quaternion.LookRotation(hitNormal));
            if (isScoped)
                impactObject.transform.localScale = impactObject.transform.localScale * 4;
            Destroy(impactObject, 3f);
            impactObject.transform.SetParent(colliders[0].transform);
        }
        
    }

    public IEnumerator SpawnTrail(TrailRenderer trail, Vector3 point)
    {
        float time = 0;
        Vector3 startPos = trail.transform.position;

        while (time < 1 && trail.gameObject.activeSelf)
        {
            trail.transform.position = Vector3.Lerp(startPos, point, time);
            time += Time.fixedDeltaTime / trail.time;

            yield return null;

            trail.transform.position = point;
            Destroy(trail.gameObject, trail.time);
        }
    }
}
