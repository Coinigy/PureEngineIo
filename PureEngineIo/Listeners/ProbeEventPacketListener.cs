using PureEngineIo.Exceptions;
using PureEngineIo.Interfaces;
using PureEngineIo.Parser;
using PureEngineIo.Transports.PollingImp;
using PureEngineIo.Transports.WebSocketImp;
using System;
using System.Collections.Immutable;

namespace PureEngineIo.Listeners
{
    internal class ProbeEventPacketListener : IListener
    {
        internal OnTransportOpenListener _onTransportOpenListener;

        public ProbeEventPacketListener(OnTransportOpenListener onTransportOpenListener) => _onTransportOpenListener = onTransportOpenListener;

        void IListener.Call(params object[] args)
        {
            if (_onTransportOpenListener.Parameters.Failed[0])
            {
                return;
            }

            var msg = (Packet)args[0];
            if (Packet.PONG == msg.Type && "probe" == (string)msg.Data)
            {
                //log.Info(
                //    string.Format("probe transport '{0}' pong",
                //        _onTransportOpenListener.Parameters.Transport[0].Name));

                _onTransportOpenListener.Parameters.Socket.Upgrading = true;
                _onTransportOpenListener.Parameters.Socket.Emit(PureEngineIoSocket.EVENT_UPGRADING, _onTransportOpenListener.Parameters.Transport[0]);
                PureEngineIoSocket.PriorWebsocketSuccess = WebSocket.NAME == _onTransportOpenListener.Parameters.Transport[0].Name;

                //log.Info(
                //    string.Format("pausing current transport '{0}'",
                //        _onTransportOpenListener.Parameters.Socket.Transport.Name));
                ((Polling)_onTransportOpenListener.Parameters.Socket.Transport).Pause(
                    () =>
                    {
                        if (_onTransportOpenListener.Parameters.Failed[0])
                        {
                                // reset upgrading flag and resume polling
                                ((Polling)_onTransportOpenListener.Parameters.Socket.Transport).Resume();
                            _onTransportOpenListener.Parameters.Socket.Upgrading = false;
                            _onTransportOpenListener.Parameters.Socket.Flush();
                            return;
                        }
                        if (ReadyStateEnum.CLOSED == _onTransportOpenListener.Parameters.Socket.ReadyState ||
                            ReadyStateEnum.CLOSING == _onTransportOpenListener.Parameters.Socket.ReadyState)
                        {
                            return;
                        }

                            //TODO: logging
                            Console.WriteLine("changing transport and sending upgrade packet");

                        _onTransportOpenListener.Parameters.Cleanup[0]();

                        _onTransportOpenListener.Parameters.Socket.SetTransport(_onTransportOpenListener.Parameters.Transport[0]);
                        var packetList = ImmutableList<Packet>.Empty.Add(new Packet(Packet.UPGRADE));
                        try
                        {
                            _onTransportOpenListener.Parameters.Transport[0].Send(packetList);

                            _onTransportOpenListener.Parameters.Socket.Upgrading = false;
                            _onTransportOpenListener.Parameters.Socket.Flush();

                            _onTransportOpenListener.Parameters.Socket.Emit(PureEngineIoSocket.EVENT_UPGRADE, _onTransportOpenListener.Parameters.Transport[0]);
                            _onTransportOpenListener.Parameters.Transport = _onTransportOpenListener.Parameters.Transport.RemoveAt(0);

                        }
                        catch (Exception e)
                        {
                            //TODO: logging
                            Console.WriteLine(e.Message);
                        }

                    });
            }
            else
            {
                Console.WriteLine(string.Format("probe transport '{0}' failed", _onTransportOpenListener.Parameters.Transport[0].Name));

                var err = new PureEngineIOException("probe error");
                _onTransportOpenListener.Parameters.Socket.Emit(PureEngineIoSocket.EVENT_UPGRADE_ERROR, err);
            }

        }
        
        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
