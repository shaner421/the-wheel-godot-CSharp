using Godot;
using System;
using System.Collections.Generic;  
[Tool]
public partial class Wheel : Control
{
	[Signal]
	public delegate void newDirSelectedEventHandler(); // emitted when a new direction is selected.
	[Signal]
	public delegate void newDirChosenEventHandler(int[] payload); // emitted when a direction is chosen.
	[Signal]
	public delegate void rotationStartedEventHandler(); // emitted when the gimbal begins to be rotated.
	[Signal]
	public delegate void rotationFinishedEventHandler(); // emitted when the gimbal is finished rotating.
	[Signal]
	public delegate void puzzleFinishedEventHandler(); // emitted when the puzzle is complete. 

	[ExportCategory("Wheel Cosmetics")]
	[ExportGroup("Size & Scale")]
	[Export]
	public Vector2 wheelSize = new Vector2(300,300); // define's the wheel's custom minimum size
	[Export(PropertyHint.Range, "0,5,0.1")]
	public float wheelScale = 1f; // specifies the wheel's scale.
	[ExportGroup("Animations")]
	[Export]
	TweenType tweenType = TweenType.TRANS_CIRC; // controls the animation of the rotation of the wheel.
	public enum TweenType {
		TRANS_LINEAR, // linear animation.
		TRANS_SINE, // sine animation.
		TRANS_QUINT, // quint animation.
		TRANS_QUART, // quart animation.
		TRANS_QUAD, // quad animation.
		TRANS_EXPO, // expo animation.
		TRANS_ELASTIC, // elastic animation.
		TRANS_CUBIC, // cubic animation.
		TRANS_CIRC, // circ animation.
		TRANS_BOUNCE, // bounce animation.
		TRANS_BACK, // back animation.
		TRANS_SPRING // spring animation.
	};
	[Export(PropertyHint.Range, "0,2,0.05")]
	float animTime = 0.3f;
	[ExportGroup("Textures")]
	[Export]
	Texture2D[] sliceTextures;
	[Export]
	Texture2D underlayTexture = GD.Load<Texture2D>("res://the-wheel-godot-CSharp/Assets/wheel-simple/underlay.png");
	[Export]
	Texture2D overlayTexture = GD.Load<Texture2D>("res://the-wheel-godot-CSharp/Assets/wheel-simple/overlay.png");
	[Export]
	Texture2D selectorTexture = GD.Load<Texture2D>("res://the-wheel-godot-CSharp/Assets/wheel-simple/selector.png");
	
	TextureRect[] slices;
	TextureRect[] covers;
	TextureRect overlay;
	TextureRect underlay;
	TextureRect selector;
	Control sliceGimbal;
	
	public int[] baseNumbers = new int[4]{-2,-1,1,2}; // base score values for the slices
	public int[] sliceValues = new int[4]{1,2,3,4}; // slice value multiplier
	public int[] currentValueMappings = new int[4]{0,90,180,270}; // assigns values to directions; format is [UP,RIGHT,DOWN,LEFT]
	enum WheelState {AWAITING_SELECTION,ROTATING,NO_INPUT};
	WheelState _state = WheelState.AWAITING_SELECTION;
	public int numSelections = 0;
	public WheelPayload _currentValue;
	int currentDirection = 0;
	int[] directions = new int[4]{0,90,180,270};
	public int targetSelections = 4;

	public override void _Ready(){
		// initialize onready variables
		sliceTextures = new Texture2D[4]{
		GD.Load<Texture2D>("res://the-wheel-godot-CSharp/Assets/wheel-simple/slice1.png"),
		GD.Load<Texture2D>("res://the-wheel-godot-CSharp/Assets/wheel-simple/slice2.png"),
		GD.Load<Texture2D>("res://the-wheel-godot-CSharp/Assets/wheel-simple/slice3.png"),
		GD.Load<Texture2D>("res://the-wheel-godot-CSharp/Assets/wheel-simple/slice4.png")
		};
		slices = new TextureRect[4]{GetNode<TextureRect>("%slice1"),GetNode<TextureRect>("%slice2"),GetNode<TextureRect>("%slice3"),GetNode<TextureRect>("%slice4")};
		covers = new TextureRect[4]{GetNode<TextureRect>("%cover1"),GetNode<TextureRect>("%cover2"),GetNode<TextureRect>("%cover3"),GetNode<TextureRect>("%cover4")};
		overlay = GetNode<TextureRect>("%overlay");
		underlay = GetNode<TextureRect>("%underlay");
		selector = GetNode<TextureRect>("%selector");
		sliceGimbal = GetNode<Control>("%sliceGimbal");
		UpdateSlicesUI(sliceTextures);
		UpdateNodeUI(overlay,overlayTexture);
		UpdateNodeUI(underlay,underlayTexture);
		UpdateNodeUI(selector,selectorTexture);
		Reset();
		this.rotationFinished += () => EndCheck();
	} 
	public override void _UnhandledInput(InputEvent _event){
		if(_state!=WheelState.AWAITING_SELECTION){return;}
		if(Input.IsActionJustPressed("ui_up")){
			currentDirection = 0;
			ProcessDirectionInput(currentDirection);
		}
		if(Input.IsActionJustPressed("ui_down")){
			currentDirection = 180;
			ProcessDirectionInput(currentDirection);
		}
		if(Input.IsActionJustPressed("ui_left")){
			currentDirection = 270;
			ProcessDirectionInput(currentDirection);
		}
		if(Input.IsActionJustPressed("ui_right")){
			currentDirection = 90;
			ProcessDirectionInput(currentDirection);
		}
		if(Input.IsActionJustPressed("ui_text_completion_replace")){
			RotateSlices();
		}
		if(Input.IsActionJustPressed("ui_accept")){
			ProcessConfirmInput(currentDirection);
		}
	}
	void ProcessDirectionInput(int dir){
		if(_state!=WheelState.AWAITING_SELECTION){return;}
		selector.RotationDegrees = dir;
		_currentValue = GetCurrentWheelValue();
		EmitSignal(SignalName.newDirSelected);
	}
	void ProcessConfirmInput(int dir){
		if(_state!=WheelState.AWAITING_SELECTION){return;}
		foreach(Control x in covers){
			if((Mathf.Round(x.RotationDegrees)) == dir){
				if(x.Visible){return;}
				x.Visible = true;
				numSelections++;
				int i = _currentValue.baseValue;
				int j = _currentValue.sliceValue;
				int k = _currentValue.totalValue;
				int[] payload = new int[3]{i,j,k};
				EmitSignal(SignalName.newDirChosen,payload);
				RotateSlices();
			}
		}
	}
	void RotateSlices(){
		if(_state!=WheelState.AWAITING_SELECTION){return;}
		_state = WheelState.ROTATING;
		RotateArray(currentValueMappings);
		_currentValue = GetCurrentWheelValue();
		EmitSignal(SignalName.rotationStarted);
		Tween tween = CreateTween();
		tween.SetTrans((Godot.Tween.TransitionType)tweenType);
		tween.TweenProperty(sliceGimbal,"rotation_degrees",sliceGimbal.RotationDegrees+90,animTime);
		tween.Finished += () => EmitSignal(SignalName.rotationFinished);
		
	}
	void Reset(){
		GD.Randomize();
		selector.SetRotationDegrees(0);
		sliceGimbal.SetRotationDegrees(0);
		numSelections = 0;
		foreach(Control x in covers){x.Visible = false;}
		Shuffle(currentValueMappings);
		for(int x = 0;x<directions.Length;x++){
			for(int j=0;j<currentValueMappings.Length;j++){
				slices[j].RotationDegrees = currentValueMappings[j];
				if(directions[x]==currentValueMappings[j]){
					sliceValues[x] = x+1;
				}
			}
		}
		Shuffle(baseNumbers);
		_currentValue = GetCurrentWheelValue();
		_state = WheelState.AWAITING_SELECTION;
	}
	void EndCheck(){
		if(numSelections==targetSelections){
			_state = WheelState.NO_INPUT;
			EmitSignal(SignalName.puzzleFinished);
		}else{
			_state = WheelState.AWAITING_SELECTION;
		}
	}
	WheelPayload GetCurrentWheelValue(){
		WheelPayload wp = new WheelPayload();
		for(int x=0;x<currentValueMappings.Length;x++){
			if(currentDirection == currentValueMappings[x]){
				wp.baseValue = baseNumbers[x];
				wp.sliceValue = sliceValues[x];
				wp.totalValue = wp.baseValue * wp.sliceValue;
			}
		}
		return wp;
	}
	void RotateArray(int[] a){
		Dictionary<int,int> numberMap = new Dictionary<int,int>();
		numberMap.Add(a[0],baseNumbers[0]);
		numberMap.Add(a[1],baseNumbers[1]);
		numberMap.Add(a[2],baseNumbers[2]);
		numberMap.Add(a[3],baseNumbers[3]);
		
		for(int x=0;x<a.Length;x++){
			a[x]+=90;
			if((int)a[x] == 360){a[x]=0;}
			baseNumbers[x] = numberMap[a[x]];
		
		}
	}
	void UpdateSlicesUI(Texture2D[] newTextures){
		for(int x=0;x<slices.Length;x++){
			slices[x].Texture = newTextures[x];
			slices[x].PivotOffset = new Vector2(newTextures[x].GetSize().X/2f,newTextures[x].GetSize().Y/2f);
			GD.Print(slices[x].PivotOffset);
			slices[x].Position = new Vector2(0,0);
			slices[x].SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
			slices[x].Size = newTextures[x].GetSize();

			covers[x].Texture = newTextures[3];
			covers[x].Modulate = new Color("000000a8");
			covers[x].PivotOffset = new Vector2(newTextures[3].GetSize().X/2f,newTextures[3].GetSize().Y/2f);
			covers[x].Position = new Vector2(0,0);
			covers[x].SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
			covers[x].Size = newTextures[3].GetSize();
		}
	}
	void UpdateNodeUI(TextureRect node,Texture2D newTexture){
		if(node==null){return;}
		node.Texture = newTexture;
		node.PivotOffset = new Vector2(newTexture.GetSize().X/2,newTexture.GetSize().Y/2);
		node.Position = new Vector2(0,0);
		node.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
		node.Size = newTexture.GetSize();
	}
	public static void Shuffle<T>(T[] array){
		Random random = new Random();
		int n = array.Length;
		for (int i = n - 1; i > 0; i--){
			// Pick a random index from 0 to i
			int j = random.Next(0, i + 1);
			// Swap array[i] with array[j]
			T temp = array[i];
			//GD.Print(temp);
			array[i] = array[j];
			array[j] = temp;
		}
	}
}

public partial class WheelPayload{
	public int baseValue;
	public int sliceValue;
	public int totalValue;
}
