using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace Voice;

public partial class VoicePlayer : Node
{
	private AudioStreamPlayer _audioStreamPlayer;
	[ExportGroup("External Objects")]
	[Export] private AudioStream _voiceStream;
	[Export] private Label _voiceLabel;
	[ExportGroup("Pause Intervals")]
	[Export] private double _beepInterval = 0.075;

	[Export] private double _clearTime = 2.00;

	[Export] private Array<BreakInterval> _breakIntervals = 
		[
			new BreakInterval(0.2, " ", false, false), 
			new BreakInterval(0.4, ".,;!?", false, true),
			new BreakInterval(0.0, "-", false, true)
		];
	
	// Test Code
	[ExportGroup("Debug Settings")]
	[Export] private string _testVoiceLine = "";
	[Export] private double _testVoiceLineDelay;
	
	// Internal State Variables, Queued up Plays
	private double _clock;
	private double _nextBeep = -1;

	private struct BeepTime
	{
		public double Time;
		public char Character;

		public BeepTime(double time, char character)
		{
			Character = character;
			Time = time;
		}
	}
	
	private readonly Queue<BeepTime> _beepTimes = [];

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		foreach (var breakInterval in _breakIntervals)
		{
			breakInterval.Initialize();
		}
		
		_audioStreamPlayer = GetChild<AudioStreamPlayer>(0);
		
		_audioStreamPlayer.Stream = _voiceStream;

		if (_testVoiceLine.Length > 0)
		{
			VoiceLine(_testVoiceLine, _testVoiceLineDelay);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_clock += delta;
		if (!ShouldBeep()) return;
		_audioStreamPlayer.Stop();
		_audioStreamPlayer.Play();
	}

	private bool ShouldBeep()
	{
		if (!_beepTimes.TryPeek(out var nextBeep)) return false;
		if (!(nextBeep.Time <= _clock)) return false;
		_beepTimes.Dequeue();
		var beep = true;

		if (nextBeep.Character != (char)0)
		{
			_voiceLabel.Text += nextBeep.Character;
			
			foreach (var interval in _breakIntervals)
			{
				if (interval.CharSet.Contains(nextBeep.Character) && !interval.ShouldBeep)
				{
					beep = false;
				}
			}
		}
		else
		{
			_voiceLabel.Text = "";
			beep = false;
		}
		
		return beep;
	}
	
	// ReSharper disable once MemberCanBePrivate.Global
	public void VoiceLine(string lineToVoice, double extraDelay = 0.0)
	{
		if (_nextBeep < _clock + extraDelay)
		{
			_nextBeep = _clock + extraDelay;
		}

		foreach (var character in lineToVoice)
		{
			var nextInterval = _beepInterval;
			var breakAfter = false;
			foreach (var interval in _breakIntervals)
			{
				if (!interval.CharSet.Contains(character)) continue;
				nextInterval = interval.Interval;
				breakAfter = interval.BreakAfter;
				break;
			}


			if (!breakAfter)
			{
				_nextBeep += nextInterval;
			}
			_beepTimes.Enqueue(new BeepTime(_nextBeep, character));
			if (breakAfter)
			{
				GD.Print("Breaking After");
				_nextBeep += nextInterval;
			}
		}

		_nextBeep += _clearTime;
		_beepTimes.Enqueue(new BeepTime(_nextBeep, (char)0));
	}
}