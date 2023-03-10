using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PlayerMovement : Damageable{
    [Header("Player stats")]
    public float moveSpeed = 5f;
    public int maxHealth = 20;
    public float currentHealth = 0;
    public int currentBlood;
    public float currentTime;

    [Header("References")]
    public Joystick joystick;
    public Joystick weaponJoystick;
    public Rigidbody2D rb;
    [SerializeField] Transform shootPoint;
    public Camera cam;
    public Animator anim;
    public TMP_Text ammoText;
    public TMP_Text timeText;
    public HealthBar healthBar;
    [SerializeField] GameObject damageEffect;
    public GameObject deathText;
    public GameObject shopMenu;
    [SerializeField] GameObject shopPrefab;
    public Transform itemPos;
    [SerializeField] Color selectedColor;
    [SerializeField] Color unselectedColor;
    [SerializeField] Image[] ImageBackgrounds;
    public Image[] itemImages;
    public TMP_Text bloodText;
    float speedMultiplier = 1f;
    [SerializeField] Item[] items;
    GameObject currentItem;
    public GameObject starterItem;
    public int itemIndex;
    int previousItemIndex = -1;
    [HideInInspector] public float damageMultiplier = 1f;
    bool dead = false;

    bool drunk = false;

    public Transform drunkPos;

    Vector2 input;
    Vector2 mousePos;

    void Awake(){
        healthBar.SetMaxHealth(maxHealth);
        currentHealth = maxHealth;
        foreach(Image i in ImageBackgrounds){
            i.color = unselectedColor;
        }
        EquipItem(0);
        if(starterItem != null){
            PickUpItem(starterItem, itemIndex);
        }
        FindObjectOfType<AudioManager>().Play("SpawnIn");
        FindObjectOfType<AudioManager>().Play("Music");
    }

    public void ReloadButton(){
        if(items[itemIndex] != null){
            items[itemIndex].Reload();
        }
    }
    public void PickupButton(){
        Interact();
    }

    void Update(){
        if(dead)
            return;
        for(int i = 0; i < items.Length; i++){
            if(Input.GetKeyDown((i + 1).ToString())){
                EquipItem(i);
                break;
            }
        }

        if(Input.GetKeyDown(KeyCode.R) && items[itemIndex] != null){
            items[itemIndex].Reload();
        }
        if(items[itemIndex] != null && items[itemIndex].IsHold() && items[itemIndex] != null){
            //if(SystemInfo.deviceType == DeviceType.Handheld){
            if(Mathf.Abs(weaponJoystick.Horizontal) > 0.2 || Mathf.Abs(weaponJoystick.Vertical) > 0.2){
                items[itemIndex].Use();
            }
            //}else{
            //    if(Input.GetMouseButton(0)){
            //        items[itemIndex].Use();
            //    }
            //}
        }else{
            if(items[itemIndex] != null && !shopMenu.activeSelf){
                //if(SystemInfo.deviceType == DeviceType.Handheld){
                    if(Mathf.Abs(weaponJoystick.Horizontal) > 0.2 || Mathf.Abs(weaponJoystick.Vertical) > 0.2){
                        items[itemIndex].Use();
                    }
                //}else{
                //    if(Input.GetMouseButtonDown(0)){
                //        items[itemIndex].Use();
                //    }
                //}
            }else if(items[itemIndex] != null && Input.GetMouseButtonUp(0)){
                items[itemIndex].StopUse();
            }
        }


        if(drunk){
            if(Vector2.Distance(drunkPos.position, transform.position) < 2){
                drunkPos.position = new Vector3(transform.position.x + Random.Range(-2f, 2f), transform.position.y + Random.Range(-2f, 2f), 0);
            }
            input.x = drunkPos.position.x - transform.position.x;
            input.y = drunkPos.position.y - transform.position.y;
        }else{
            //if(SystemInfo.deviceType == DeviceType.Handheld){
                input.x = joystick.Horizontal;
                input.y = joystick.Vertical;
            //}else{
            //    joystick.gameObject.SetActive(false);
            //    input.x = Input.GetAxisRaw("Horizontal");
            //    input.y = Input.GetAxisRaw("Vertical");
            //}
        }



        MoveCam();

        if(Input.GetKeyDown(KeyCode.E)){
            Interact();
        }
        anim.SetFloat("Speed", input.normalized.sqrMagnitude * moveSpeed/5f);
        if(items[itemIndex] != null){
            ammoText.text = items[itemIndex].GetAmmo();
        }else{
            ammoText.text = "";
        }
        bloodText.text = currentBlood.ToString();
        currentTime += Time.deltaTime;
        timeText.text = currentTime.ToString("F2");
    }

    void Interact(){
        if(shopMenu.activeSelf){
            shopMenu.SetActive(false);
            FindObjectOfType<AudioManager>().Play("Close");
        }else{
            Collider2D[] Colliders = Physics2D.OverlapCircleAll(transform.position, 3);
            for(int i = 0; i < Colliders.Length; i++){
                Debug.Log("0: " + Colliders[i].gameObject);
                if(Colliders[i].GetComponent<ItemPickup>() != null){
                    PickUpItem(Colliders[i].GetComponent<ItemPickup>().Item, itemIndex);
                    Destroy(Colliders[i].gameObject);
                    break;
                }else if(Colliders[i].GetComponent<Shop>() != null){
                    shopMenu.SetActive(true);
                    FindObjectOfType<AudioManager>().Play("Open");
                    LoadShop(Colliders[i].GetComponent<Shop>());
                    break;
                }
            }
        }
    }

    public void StartSpeed(float _speedMultiplier, float speedTime){
        Debug.Log("Start speed");
        speedMultiplier = _speedMultiplier;
        Invoke("DisableSpeed", speedTime);
    }

    void DisableSpeed(){
        speedMultiplier = 1f;
    }

    public void StartDrunk(float nauseaTime, float damageTime, float _damageMultiplier){
        drunkPos.position = new Vector3(transform.position.x + Random.Range(-2f, 2f), transform.position.y + Random.Range(-2f, 2f), 0);
        drunk = true;
        StartCoroutine(DisableDrunk(_damageMultiplier, damageTime, nauseaTime));
    }

    IEnumerator DisableDrunk(float _damageMultiplier, float damageTime, float delayTime){
        yield return new WaitForSeconds(delayTime);
        drunk = false;
        damageMultiplier = _damageMultiplier;
        StartCoroutine(DisableDamage(damageTime));
    }

    IEnumerator DisableDamage(float delayTime){
        yield return new WaitForSeconds(delayTime);
        damageMultiplier = 1f;
    }


    void LoadShop(Shop shop){
        foreach(Transform child in shopMenu.transform){
            Destroy(child.gameObject);
        }
        for(int i = 0; i < shop.items.Count; i++){
            GameObject obj = Instantiate(shopPrefab, Vector3.zero, Quaternion.identity, shopMenu.transform);
            obj.GetComponent<ShopDisplay>().Init(shop.items[i]);
        }
    }

    public void PickUpItem(GameObject item, int _index){
        Debug.Log("2");
        if(items[_index] != null){
            Destroy(items[_index].gameObject);
        }
        currentItem = Instantiate(item, itemPos.position, itemPos.rotation, transform);
        itemImages[_index].sprite = currentItem.GetComponent<Item>().itemInfo.icon;
        if(currentItem.GetComponent<SingleShotGun>() != null){
            currentItem.GetComponent<SingleShotGun>().shootPoint = shootPoint;
        }
        items[_index] = currentItem.GetComponent<Item>();

    }

    void EquipItem(int _index){
        if(_index == previousItemIndex)
            return;

        itemIndex = _index;
        if(items[itemIndex] != null){
            items[itemIndex].gameObject.SetActive(true);
            if(items[itemIndex].GetAmmo() != ""){
                FindObjectOfType<AudioManager>().Play("SwitchWeapon");
            }
        }
        ImageBackgrounds[itemIndex].color = selectedColor;

        if(previousItemIndex != -1){
            if(items[previousItemIndex] != null){
                items[previousItemIndex].gameObject.SetActive(false);
            }
            ImageBackgrounds[previousItemIndex].color = unselectedColor;
        }

        previousItemIndex = itemIndex;
    }

    void MoveCam(){
        cam.transform.position = new Vector3(transform.position.x, transform.position.y, -10);
    }

    void FixedUpdate(){
        if(dead)
            return;
        if(!shopMenu.activeSelf){
            rb.MovePosition(rb.position + input.normalized * moveSpeed * Time.fixedDeltaTime * speedMultiplier);
        }
    }

    void LateUpdate(){
        if(dead)
            return;
        //if(SystemInfo.deviceType == DeviceType.Handheld){
            //foreach (Touch touch in Input.touches) {
            //    if(joystick.touch != touch.fingerId){
            //        mousePos = cam.ScreenToWorldPoint(touch.position);
            //        break;
            //    }
            //}
        //}else{
        //    mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        //}
        if(Mathf.Abs(weaponJoystick.Horizontal) > 0.2 || Mathf.Abs(weaponJoystick.Vertical) > 0.2){
            Vector2 lookDir = weaponJoystick.Direction;
            float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    public override void TakeDamage(float damage){
        if(dead)
            return;
        currentHealth -= damage;
        healthBar.SetHealth(currentHealth);
        damageEffect.SetActive(true);
        Invoke("DisableEffect", 0.2f);
        if(currentHealth <= 0){
            FindObjectOfType<AudioManager>().Play("Die");
            deathText.SetActive(true);
            dead = true;
            if(PlayerPrefs.GetInt("Highscore") < currentTime){
                PlayerPrefs.SetInt("Highscore", (int)currentTime);
            }
            Invoke("LeaveGame", 2f);
        }else{
            FindObjectOfType<AudioManager>().Play("Oof");
        }
    }
    void DisableEffect(){
        damageEffect.SetActive(false);
    }

    void LeaveGame(){
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }
}























