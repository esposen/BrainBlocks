﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
public class MentalCommandControl : MonoBehaviour {
    //UI
    public Button btnNeutral, btnLeft, btnRight,
                  btnNeutralClear, btnLeftClear,
                  btnRightClear, btnNext;
    public Slider slider;
    public TextMeshProUGUI trainPercentage,curAction,status,
                           btnRtrialText, btnLtrialText;
    public GameObject leftPrompt, rightPrompt, leftCheckmark, rightCheckmark,
                       trialInfoPanel, acceptTrainPanel, clearPanel;
    //Control
    public TrainingCube cube;
    uint userId;
    public bool firstTime = true;
    bool training, neutralDone, leftFirst, inputRecieved, acceptTraining = false;
    public string trainType;
	EmoEngine engine;


    //XX Start For testing without EMOTIV
    //private void Update()
    //{
    //    if (Input.GetKey(KeyCode.LeftArrow))
    //        cube.action = cube.ACTION_LEFT;
    //    else if (Input.GetKey((KeyCode.RightArrow)))
    //        cube.action = cube.ACTION_RIGHT;
    //    else
    //        cube.action = cube.ACTION_NEUTRAL;
    //}
    //private void Start()
    //{
    //    CustomStart();
    //}
    // XX End

    //------------------------------Emotiv Event Functions------------------------------//

    void BindEvents(){
        engine.UserAdded += OnUserAdded;
        engine.MentalCommandTrainingStarted += OnTrainingStarted;
        engine.MentalCommandTrainingSucceeded += OnTrainingSuccess;
        engine.MentalCommandTrainingCompleted += OnTrainingAccepted;
        engine.MentalCommandTrainingRejected += OnTrainingRejected;
        engine.MentalCommandEmoStateUpdated += OnMentalCommandEmoStateUpdated;
    }

    void UnbindEvents(){
		engine.UserAdded -= OnUserAdded;
		engine.MentalCommandTrainingStarted -= OnTrainingStarted;
		engine.MentalCommandTrainingSucceeded -= OnTrainingSuccess;
		engine.MentalCommandTrainingCompleted -= OnTrainingAccepted;
		engine.MentalCommandTrainingRejected -= OnTrainingRejected;
		engine.MentalCommandEmoStateUpdated -= OnMentalCommandEmoStateUpdated;
    }

    //Move cube and update Current Action UI according to new mental action
    void OnMentalCommandEmoStateUpdated(object sender, EmoStateUpdatedEventArgs args){
        if (training) return; //do not update during training
        EdkDll.IEE_MentalCommandAction_t action = args.emoState.MentalCommandGetCurrentAction();
        Debug.Log("Training: State Updated " + action);
        //Move Block and Update UI text
        switch (action)
        {
            case EdkDll.IEE_MentalCommandAction_t.MC_NEUTRAL:
                cube.action = 0;
                curAction.text = "Current Action: Neutral";
                break;
            case EdkDll.IEE_MentalCommandAction_t.MC_RIGHT:
                cube.action = 1;
                curAction.text = "Current Action: Right";
                break;
            case EdkDll.IEE_MentalCommandAction_t.MC_LEFT:
                cube.action = 2;
                curAction.text = "Current Action: Left";
				break;

        }
    }

	//Event function called by EmoEngine when EPOCH is connected
	private void OnUserAdded(object sender, EmoEngineEventArgs args){
        userId = args.userId;
    }

	//Event function called by EmoEngine before training period starts
	void OnTrainingStarted(object sender, EmoEngineEventArgs args)
    {
        status.text = "Training " + trainType;
        training = true;
        ActivateButtons(false);
    }

    //Event function called by EmoEngine when training period ends
    void OnTrainingSuccess(object sender, EmoEngineEventArgs args){
		Debug.Log("In Success");
		StartCoroutine(AcceptTraining());
    }

	//Event function called by EmoEngine when training is accepted
	void OnTrainingAccepted(object sender, EmoEngineEventArgs args){
		Debug.Log("In Accepted0");
		status.text = "Success! Training " + trainType + " Concluded";
		training = false;
		Debug.Log("In Accepted1");
		ActivateButtons(true);
		Debug.Log("In Accepted2");
		if (!firstTime || trainType == "Neutral")
		{
			Debug.Log("In Accepted3");
			ResetCube();
		}
		//Shows information about training trials after
		// accepting the first right or left data
		if (firstTime && trainType != "Neutral")
		{
			Debug.Log("In Accepted4");
			firstTime = false;
			Debug.Log("In Accepted5");
			trialInfoPanel.SetActive(true);
			Debug.Log("In Accepted6");

		}
	}

	//Event function called by EmoEngine when training is rejected
	void OnTrainingRejected(object sender, EmoEngineEventArgs args){
		Debug.Log("In Rejected");
		status.text = trainType + " Data Rejected";
        cube.action = cube.ACTION_RESET;
		ActivateButtons(true);
        training = false;
	} 

    //Waits on user to commit or reject most recent training data
	IEnumerator AcceptTraining()
	{
        acceptTrainPanel.SetActive(true);
		while (!inputRecieved) yield return null;
		if (acceptTraining) {
            Debug.Log("Coroutine: ACCEPTING");
            LoggerCSV.GetInstance().AddEvent(LoggerCSV.EVENT_TRAINING_ACCEPT);
			engine.MentalCommandSetTrainingControl(userId, EdkDll.IEE_MentalCommandTrainingControl_t.MC_ACCEPT);
		}
		else{
			Debug.Log("Coroutine: REJECTING");
			LoggerCSV.GetInstance().AddEvent(LoggerCSV.EVENT_TRAINING_REJECT);
            engine.MentalCommandSetTrainingControl(userId, EdkDll.IEE_MentalCommandTrainingControl_t.MC_REJECT);
		}
        inputRecieved = false;
	}

    //------------------------------UI Helper Functions------------------------------//
    //Coroutine to control Training_Slider
    //Takes 8 seconds to get to 100%
    IEnumerator UpdateSlider()
    {
        //XX Start for testing without emotiv
        //object s = null;
        //EmoEngineEventArgs a = null;
        //OnTrainingStarted(s, a);
        //XX END

        while (true)
        {
            slider.value += Time.deltaTime;
            int percent = (int)(100 * (slider.value / slider.maxValue));
            trainPercentage.text = percent.ToString() + "%";
            if (percent == 100) break;
            else yield return null;
        }
        yield return new WaitForSeconds(1f);
        slider.value = 0;
        trainPercentage.text = "0%";

        //XX Start for testing without emotiv
        //OnTrainingSuccessed(s, a);
        //OnTrainingCompleted(s, a);
        //XX END


    }

    //Shows/Hides UI objects depending on state
    public void UpdateUI (string state) {
        if (cube.leftDone && cube.rightDone)
            btnNext.gameObject.SetActive(true);
        else
            btnNext.gameObject.SetActive(false);
        Debug.Log("UpdateUI("+state+")");
        switch(state){
            case "Neutral":
                btnNeutralClear.gameObject.SetActive(true);
                if (leftFirst)
                    btnLeft.gameObject.SetActive(true);
                else
                    btnRight.gameObject.SetActive(true);
                neutralDone = true;
				break;
            case "clear neutral":
                btnNeutralClear.gameObject.SetActive(false);
                btnRight.gameObject.SetActive(false);
                btnRightClear.gameObject.SetActive(false);
                btnLeft.gameObject.SetActive(false);
                btnLeftClear.gameObject.SetActive(false);
                rightCheckmark.gameObject.SetActive(false);
                leftCheckmark.gameObject.SetActive(false);
				neutralDone = false;
                cube.lefttrial = false;
                cube.righttrial = false;
                cube.leftDone = false;
                cube.rightDone = false;
                ActivateButtons(true);
                break;
            case "Right":
                btnRightClear.gameObject.SetActive(true);
                rightPrompt.SetActive(true);
                rightCheckmark.SetActive(false);
                cube.righttrial = true;
                break;
            case "clear right":
                btnRightClear.gameObject.SetActive(false);
                rightPrompt.SetActive(false);
                rightCheckmark.gameObject.SetActive(false);
                cube.righttrial = false;
                cube.rightDone = false;
                break;
            case "Left":
                btnLeftClear.gameObject.SetActive(true);
                leftPrompt.SetActive(true);
                leftCheckmark.SetActive(false);
                cube.lefttrial = true;
                break;
            case "clear left":
                btnLeftClear.gameObject.SetActive(false);
                leftPrompt.SetActive(false);
                leftCheckmark.SetActive(false);
                cube.lefttrial = false;
                cube.leftDone = false;
                break;
            case "done left":
                status.text = "Left Command Adequately Trained";
                leftCheckmark.SetActive(true);
                leftPrompt.SetActive(false);
                btnLeft.interactable = false;
                btnLeftClear.interactable = false;
                if (leftFirst)
                    btnRight.gameObject.SetActive(true);
                break;
            case "done right":
                status.text = "Right Command Adequately Trained";
				rightCheckmark.SetActive(true);
                rightPrompt.SetActive(false);
                btnRight.interactable = false;
				btnRightClear.interactable = false;
                if (!leftFirst)
                    btnLeft.gameObject.SetActive(true);
                break;
		}
    }

    //Decativates/Activates buttons depending on training
    void ActivateButtons(bool yes){
        Debug.Log("ActivateButtons("+yes+")");
		if(!neutralDone)
            btnNeutral.interactable = yes;
        if (!cube.leftDone)
        {
            btnLeft.interactable = yes;
            btnLeftClear.interactable = yes;
        }
        if (!cube.rightDone)
        {
            btnRight.interactable = yes;
            btnRightClear.interactable = yes;
        }
        btnNeutralClear.interactable = yes;
    }

//------------------------------UI OnClick Functions------------------------------//

    //Called by Start_Training_Button
    public void CustomStart(){
        LoggerCSV logger = LoggerCSV.GetInstance();
        logger.AddEvent(LoggerCSV.EVENT_TRAINSTAGE_START);
        cube = GameObject.Find("Block").GetComponent<TrainingCube>();
        leftFirst = logger.counterBalanceID == 1 
                          || logger.counterBalanceID == 2;
		slider.value = 0;
		engine = EmoEngine.Instance;
		BindEvents();

    }

    //Called by Left_Button, Right_Button, and Neutral_Button
	public void TrainAction(string type)
	{
        LoggerCSV logger = LoggerCSV.GetInstance();
		trainType = type;
        EdkDll.IEE_MentalCommandAction_t toTrain = EdkDll.IEE_MentalCommandAction_t.MC_NEUTRAL;
        switch (type)
        {
            case "Left":
                cube.action = cube.ACTION_LEFT;
                toTrain = EdkDll.IEE_MentalCommandAction_t.MC_LEFT;
                logger.AddEvent(LoggerCSV.EVENT_TRAINING_L);
                break;
            case "Right":
                cube.action = cube.ACTION_RIGHT;
                toTrain = EdkDll.IEE_MentalCommandAction_t.MC_RIGHT;
				logger.AddEvent(LoggerCSV.EVENT_TRAINING_R);
				break;
            default:
                logger.AddEvent(LoggerCSV.EVENT_TRAINING_N);
                break;
        }

		StartCoroutine(UpdateSlider());

        if (type != "Neutral")
        {
            EmoMentalCommand.EnableMentalCommandAction(toTrain, true);
            EmoMentalCommand.EnableMentalCommandActionsList();
        }
        EmoMentalCommand.StartTrainingMentalCommand(toTrain);
	}

    //Called by Clear_Neutral_Button, Clear_Right_Button & Clear_Left_Button
    public void ClearTrainingCheck(string type)
    {
        switch (type)
        {
            case "Left":
                trainType = "clear left";
                break;
            case "Right":
                trainType = "clear right";
                break;
            default:
                trainType = "clear neutral";
                break;
        }
        clearPanel.SetActive(true);

    }

	//Called by Accept_Clear_Button
	public void ClearTraining(){
        LoggerCSV logger = LoggerCSV.GetInstance();
        string statusText = "Neutral";
		EdkDll.IEE_MentalCommandAction_t action = EdkDll.IEE_MentalCommandAction_t.MC_NEUTRAL;
        switch (trainType)
		{
			case "clear left":
                statusText = "Left";
				action = EdkDll.IEE_MentalCommandAction_t.MC_LEFT;
				trainType = "clear left";
                logger.AddEvent(LoggerCSV.EVENT_TRAINING_CLEAR_L);
                break;
			case "clear right":
                statusText = "Right";
				action = EdkDll.IEE_MentalCommandAction_t.MC_RIGHT;
				trainType = "clear right";
				logger.AddEvent(LoggerCSV.EVENT_TRAINING_CLEAR_R);
				break;
            default:
                logger.AddEvent(LoggerCSV.EVENT_TRAINING_CLEAR_N);
                status.text = "Current Aciton: None";
                break; 
		}
		EdkDll.IEE_MentalCommandSetTrainingAction((uint)EmoUserManagement.currentUser, action);
		EdkDll.IEE_MentalCommandSetTrainingControl((uint)EmoUserManagement.currentUser,
												   EdkDll.IEE_MentalCommandTrainingControl_t.MC_ERASE);
		UpdateUI(trainType);
		status.text = "Cleared " + statusText + " Training Data";
    }

    //Called by Accept_Training_Button and Reject Training Button
    public void AcceptTrainClick(bool yes){
        inputRecieved = true;
        acceptTraining = yes;
    }

    //Called by Next_Scene_Button
    public void NextScene(){
        LoggerCSV.GetInstance().AddEvent(LoggerCSV.EVENT_TRAINSTAGE_END);
        UnbindEvents();
        SceneManager.LoadScene(2);
    }

	//Called by Continue_Training_Button
    //Ensures Cube is reset before starting trials
	public void ResetCube(){
        cube.action = cube.ACTION_RESET;
        UpdateUI(trainType);
    }
}