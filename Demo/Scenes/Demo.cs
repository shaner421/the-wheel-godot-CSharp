using Godot;
using System;

public partial class Demo : Control
{
	
	Wheel wheel;
	AudioStream backgroundMusic = GD.Load<AudioStream>("uid://0g3hbfl4uosc");
	AudioStream selectSound = GD.Load<AudioStream>("uid://cxg4q58es2u77");
	AudioStream rotateSound = GD.Load<AudioStream>("uid://c43qhby2kqxxj");
	AudioStream successSound = GD.Load<AudioStream>("uid://bd4rwxqynrent");
	AudioStream failSound = GD.Load<AudioStream>("uid://pcg4xnx3rd1t");
	int currentWheelValue = 0;
	AudioStreamPlayer bgMsc;
	bool gameOver = false;
	
	public override void _Ready(){
		wheel = GetNode<Wheel>("Wheel");
		bgMsc = new AudioStreamPlayer();
		AddChild(bgMsc);
		wheel.newDirSelected += () => _PlaySound(selectSound);
		wheel.newDirChosen += (payload) => updateWheelValue(payload);
		wheel.rotationStarted += () => _PlaySound(rotateSound);
		_PlayMusic(backgroundMusic);
	}
	public override void _Process(double delta){
		if(gameOver){return;};
		updateText();
		showValue();
		if(wheel.numSelections == wheel.targetSelections){
			endCheck(currentWheelValue);
		}
	}
	void updateWheelValue(int[] values){
		currentWheelValue += values[2];
	}
	void endCheck(int wheelVal){
		if(wheelVal > 0){
			GetNode<Control>("GameOvers/pass").Visible = true;
			_PlaySound(successSound);
		}else{
			GetNode<Control>("GameOvers/fail").Visible = true;
			_PlaySound(failSound);
		}
		gameOver = true;
		bgMsc.VolumeDb = -200;
	}
	void updateText(){
		string slice = new string("Slice Value: "+wheel._currentValue.sliceValue.ToString());
		string basev = new string("Base Value: "+wheel._currentValue.baseValue.ToString());
		string selections = new string("Number of Selections: "+wheel.numSelections.ToString());
		string bases = new string("Base Values: "+ string.Join(",", wheel.baseNumbers));
		string valueMappings = new string("Value Mappings: "+string.Join(",", wheel.currentValueMappings));
		string sliceMultipliers = new string("Slice Multipliers: "+string.Join(",", wheel.sliceValues));
		GetNode<Label>("text/wheelValue").Text = "Wheel Value: "+currentWheelValue.ToString();
		GetNode<Label>("text/sliceValue").Text = slice;
		GetNode<Label>("text/baseValue").Text = basev;
		GetNode<Label>("text/numSelections").Text = selections;
		GetNode<Label>("text/valueMap").Text = valueMappings;
		GetNode<Label>("text/baseMap").Text = bases;
		GetNode<Label>("text/sliceMap").Text = sliceMultipliers;
	}
	void showValue(){
		int b = wheel._currentValue.baseValue;
		TextureRect[] olives = new TextureRect[4]{
			GetNode<TextureRect>("values/2"),
			GetNode<TextureRect>("values/1"),
			GetNode<TextureRect>("values/-1"),
			GetNode<TextureRect>("values/-2")
			};
		ColorRect[] colors = new ColorRect[4]{
			GetNode<ColorRect>("colors/2"),
			GetNode<ColorRect>("colors/1"),
			GetNode<ColorRect>("colors/-1"),
			GetNode<ColorRect>("colors/-2")
		};
		
		for(int x=0;x<olives.Length;x++){
			if(olives[x].Name == b.ToString()){
				olives[x].Visible = true;
				colors[x].Visible = true;	
			}else{
				olives[x].Visible = false;
				colors[x].Visible = false;
			}
		}
	}
	void _PlaySound(AudioStream sound){
		GD.Randomize();
		AudioStreamPlayer player = new AudioStreamPlayer();
		player.Stream = sound;
		player.PitchScale = (float)GD.RandRange(0.95f,1.05f);
		AddChild(player);
		player.Play();
		player.Finished += () => player.QueueFree();
	}
	void _PlayMusic(AudioStream music){
		bgMsc.Stream = music;
		bgMsc.PitchScale = 1.02f;
		bgMsc.VolumeDb = -30;
		bgMsc.Play();
		// for this section I just set the goofy tune to loop in the import settings
		// rather than connecting the signal and awaiting. I got lazy. Sorry!
	}
	void _on_music_checkbox_toggled(bool toggled_on){
		if(toggled_on){
			bgMsc.VolumeDb = -30;
		}else{
			bgMsc.VolumeDb = -200;
		}
	}
	void _on_debug_checkbox_toggled(bool toggled_on){
		GetNode<Label>("text/wheelValue").Visible = toggled_on;
		GetNode<Label>("text/sliceValue").Visible = toggled_on;
		GetNode<Label>("text/baseValue").Visible = toggled_on;
		GetNode<Label>("text/numSelections").Visible = toggled_on;
		GetNode<Label>("text/valueMap").Visible = toggled_on;
		GetNode<Label>("text/baseMap").Visible = toggled_on;
		GetNode<Label>("text/sliceMap").Visible = toggled_on;
	}
}
