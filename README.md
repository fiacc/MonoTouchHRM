MonoTouchHRM
============

Wrapper around accessing a bluetooth low energy heart rate monitor on MonoTouch based on 
https://github.com/timburks/iOSHeartRateMonitor
which is based on
http://developer.apple.com/library/mac/#samplecode/HeartRateMonitor


 _monitor = new HeartRateMonitor();
 
 _monitor.HeartRateUpdated += (object hrm, HeartRateMonitor.HeartRateEventArgs ev) =>
 
                        {

                            Console.WriteLine("Heartrate::" + ev.HeartRate);
                            
                        };
                        
_monitor.Connect();
