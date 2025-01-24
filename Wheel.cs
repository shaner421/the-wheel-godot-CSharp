using Godot;
using System;
using System.Collections.Generic;  


// Hello and welcome to the Persuasion Wheel Game Jam AKA WHEELJAM! This code is
// free to use and distribute for the purposes of WHEELJAM and any games you choose
// to make with it outside of wheeljam as well. Please provide attribution if you 
// use it outside the jam. I'd love to see what you make with it!

// you can also make your own wheel if you'd like, this one is not required,
// but I did try to make it as easy to use as possible so people can plug in their
// own art and sounds and just kinda run with it. This code has been adapted from
// Colin McInerney's Unity implementation of the wheel (Well technicially this was
// adapted from the GDScript adaptation of Colin's Unity Wheel lol). 

// Have fun, tag me on bsky (@shanescott.itch.io) if you use the wheel bc again,
// I'd LOVE to see what you make, and good luck!

// Love, Shane Scott & Colin McInerney.


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
	Texture2D[] sliceTextures; // array of slice textures. Defaults to the simple ones.
	[Export]
	Texture2D underlayTexture = GD.Load<Texture2D>("res://the-wheel-godot-CSharp/Assets/wheel-simple/underlay.png"); // our underlay texture.
	[Export]
	Texture2D overlayTexture = GD.Load<Texture2D>("res://the-wheel-godot-CSharp/Assets/wheel-simple/overlay.png"); // our overlay texture.
	[Export]
	Texture2D selectorTexture = GD.Load<Texture2D>("res://the-wheel-godot-CSharp/Assets/wheel-simple/selector.png"); // our selector texture.
	
	TextureRect[] slices; // an array of our slice nodes.
	TextureRect[] covers; // an array of our cover nodes.
	TextureRect overlay; // our overlay node.
	TextureRect underlay; // our underlay node.
	TextureRect selector; // our selector node.
	Control sliceGimbal; // our slice gimbal node.
	
	public int[] baseNumbers = new int[4]{-2,-1,1,2}; // base score values for the slices
	public int[] sliceValues = new int[4]{1,2,3,4}; // slice value multiplier
	public int[] currentValueMappings = new int[4]{0,90,180,270}; // assigns values to directions; format is [UP,RIGHT,DOWN,LEFT]
	enum WheelState {AWAITING_SELECTION,ROTATING,NO_INPUT}; // enum for the state our wheel is in.
	WheelState _state = WheelState.AWAITING_SELECTION; // variable containing the current state of the wheel.
	public int numSelections = 0; // the number of times a selection has been made.
	public WheelPayload _currentValue; // the current wheel payload of the slice of the wheel that the selector is hovering over.
	int currentDirection = 0; // the selector's current direction.
	int[] directions = new int[4]{0,90,180,270}; // an array of available directions.
	public int targetSelections = 4; // the number of times a selection can be made.
	
	// called when the scene is loaded into the tree.
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
		// update our textures at runtime.
		UpdateSlicesUI(sliceTextures);
		UpdateNodeUI(overlay,overlayTexture);
		UpdateNodeUI(underlay,underlayTexture);
		UpdateNodeUI(selector,selectorTexture);
		Reset();
		
		this.rotationFinished += () => EndCheck();
	} 
	public override void _UnhandledInput(InputEvent _event){
		if(_state!=WheelState.AWAITING_SELECTION){return;}
		// if up, down, left, right is pressed, process that direction input
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
		if(Input.IsActionJustPressed("ui_text_completion_replace")){ // if tab is pressed, rotate slices
			RotateSlices();
		}
		if(Input.IsActionJustPressed("ui_accept")){ // if space is pressed, confirm selection
			ProcessConfirmInput(currentDirection);
		}
	}
	// processes the input direction and moves the selector to that direction.
	void ProcessDirectionInput(int dir){
		if(_state!=WheelState.AWAITING_SELECTION){return;}
		selector.RotationDegrees = dir; // move our selector to the direction
		_currentValue = GetCurrentWheelValue(); // set the current wheel value 
		EmitSignal(SignalName.newDirSelected); // emit signal that we have moved the selector
	}
	// confirms that the current selection has been chosen
	void ProcessConfirmInput(int dir){
		if(_state!=WheelState.AWAITING_SELECTION){return;}
		foreach(Control x in covers){ // show the covers, increase the numSelections, emit the signal, rotate
			if((Mathf.Round(x.RotationDegrees)) == dir){
				if(x.Visible){return;}
				x.Visible = true;
				numSelections++;
				int i = _currentValue.baseValue;
				int j = _currentValue.sliceValue;
				int k = _currentValue.totalValue;
				int[] payload = new int[3]{i,j,k};
				EmitSignal(SignalName.newDirChosen,payload); // we build an int[] because I can't pass a custom class (wheelpayload) thru c# signals. 
				RotateSlices();
			}
		}
	}
	// rotates the slice gimbal +90 degrees
	void RotateSlices(){
		if(_state!=WheelState.AWAITING_SELECTION){return;}
		_state = WheelState.ROTATING;
		RotateArray(currentValueMappings); // +90 degrees to each value mapping
		_currentValue = GetCurrentWheelValue(); // make sure wheel value is updated
		EmitSignal(SignalName.rotationStarted); 
		Tween tween = CreateTween(); // create tween object for animation
		tween.SetTrans((Godot.Tween.TransitionType)tweenType); // set transition type of animation
		tween.TweenProperty(sliceGimbal,"rotation_degrees",sliceGimbal.RotationDegrees+90,animTime); // rotate gimbal
		tween.Finished += () => EmitSignal(SignalName.rotationFinished); // emit rotation finished when done anim
		
	}
	// this function resets the minigame.
	void Reset(){
		selector.SetRotationDegrees(0); // remove this if you don't want the selector to reset every time
		sliceGimbal.SetRotationDegrees(0); // resets gimbal
		numSelections = 0;
		foreach(Control x in covers){x.Visible = false;} // hides all the covers
		Shuffle(currentValueMappings); // use our helper function to shuffle the values ; had to make the shuffle function for the c# implementation
		for(int x = 0;x<directions.Length;x++){ // assigns the slice value to the direction of the corresponding slice
			for(int j=0;j<currentValueMappings.Length;j++){
				slices[j].RotationDegrees = currentValueMappings[j]; // sets the slice rotation to our value mappings
				if(directions[x]==currentValueMappings[j]){
					sliceValues[x] = x+1;
				}
			}
		}
		Shuffle(baseNumbers); // shuffles base numbers so random wheel segments = a random base number 
		_currentValue = GetCurrentWheelValue(); 
		_state = WheelState.AWAITING_SELECTION;
	}
	// checks if minigame is finished
	void EndCheck(){
		if(numSelections==targetSelections){
			_state = WheelState.NO_INPUT;
			EmitSignal(SignalName.puzzleFinished);
		}else{
			_state = WheelState.AWAITING_SELECTION;
		}
	}
	// returns the wheel value
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
	// +90 degrees to each value mapping, also adjusts base values so they stay the same.
	void RotateArray(int[] a){
		Dictionary<int,int> numberMap = new Dictionary<int,int>(); // saves where our base numbers are at after reset
		numberMap.Add(a[0],baseNumbers[0]);
		numberMap.Add(a[1],baseNumbers[1]);
		numberMap.Add(a[2],baseNumbers[2]);
		numberMap.Add(a[3],baseNumbers[3]);
		
		for(int x=0;x<a.Length;x++){
			a[x]+=90; // +90 degrees to each value mapping
			if((int)a[x] == 360){a[x]=0;} // wrap back around to 0 if 360
			baseNumbers[x] = numberMap[a[x]]; // make sure our base numbers stay the same
		
		}
	}
	// this is all UI updating stuff. 
	void UpdateSlicesUI(Texture2D[] newTextures){
		for(int x=0;x<slices.Length;x++){
			// update our slices with the new texture
			slices[x].Texture = newTextures[x];
			slices[x].PivotOffset = new Vector2(newTextures[x].GetSize().X/2f,newTextures[x].GetSize().Y/2f);
			slices[x].Position = new Vector2(0,0);
			slices[x].SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
			slices[x].Size = newTextures[x].GetSize();
			// set our covers to the biggest slice at a modulate and update
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
	// this is a shuffle function I stole from stackoverflow, pls don't yell at me lol.
	// From my understanding the function takes in an array of type T bc T is a placeholder
	// for any type, so it can take in any array, and then it iterates thru the provided
	// array backwards, picking random indexes to swap. idk it works. i'm not gonna think 
	// abt it too much.
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
// allows us to create wheel payload objects and assign values to the wheel.
public partial class WheelPayload{
	public int baseValue;
	public int sliceValue;
	public int totalValue;
}
