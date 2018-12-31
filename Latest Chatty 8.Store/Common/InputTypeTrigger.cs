using Windows.Devices.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Latest_Chatty_8.Common
{
	public class InputTypeTrigger : StateTriggerBase
	{
		private FrameworkElement _targetElement;
		private PointerDeviceType _lastPointerType;
		private bool _setOnce;

		//This gets set from XAML
		public FrameworkElement TargetElement
		{
			get => _targetElement;
			set
			{
				_targetElement = value;
				_targetElement.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(PointerEvent), true);
				_targetElement.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(PointerEvent), true);
				_targetElement.AddHandler(UIElement.PointerEnteredEvent, new PointerEventHandler(PointerEvent), true);
				_targetElement.AddHandler(UIElement.PointerWheelChangedEvent, new PointerEventHandler(PointerEvent), true);
			}
		}

		//This also gets set from XAML - It is the type that will indicate if this state is on or off.
		public PointerDeviceType PointerType { get; set; }

		//Handle the events and set the state appropriately.
		private void PointerEvent(object sender, PointerRoutedEventArgs e)
		{
			//Set the initial trigger state no matter what the last type was (Since it's an enum it always has to be set to something)
			//There's no documentation on whether or not it's ok to call SetActive multiple times with the same value resulting in a noop
			//Since we're subscribing to a ton of events here, we're going to take the safe route and prevent it ourselves.
			if (e.Pointer.PointerDeviceType != _lastPointerType || _setOnce == false)
			{
				_setOnce = true;
				_lastPointerType = e.Pointer.PointerDeviceType;
				SetActive(PointerType == _lastPointerType);
			}
		}
	}
}
