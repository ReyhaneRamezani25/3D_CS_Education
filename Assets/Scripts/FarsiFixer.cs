using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ArabicSupport;
public class FarsiFixer : MonoBehaviour
{
    public Text SubText;
    string FarsiChangeHolder;
    // Start is called before the first frame update
    string holder;
    float typetexttime = 0.05f;
    void Start()
    {
        StartCoroutine(WriteTextPartlyFarsiCO());
    }

    // Update is called once per frame
    IEnumerator WriteTextPartlyFarsiCO()
    {
        FarsiChangeHolder = "";
        SubText.text = "";
        holder = "سلام من ریحانه رمضانی هستم";
        foreach (char c in holder)
        {
            FarsiChangeHolder += ArabicFixer.Fix(c.ToString());
            string tempholder = ArabicFixer.Fix(FarsiChangeHolder);
            SubText.text = tempholder;
            yield return new WaitForSeconds(typetexttime);
        }
    }
    void Update()
    {
        
    }
}
