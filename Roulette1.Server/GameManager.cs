﻿using Microsoft.AspNetCore.SignalR;
using Proto;
using Proto.Mailbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Roulette1.Server
{
    public class GameManager : IActor
    {
        Dictionary<string, User> _users = new Dictionary<string, User>();
        IHubContext<RouletteHub> _hub;
        Dictionary<string, BettingChecker> _hitChecker = new Dictionary<string, BettingChecker>();

        RandomBox<int> randomBox = new RandomBox<int>();
        public GameManager(IHubContext<RouletteHub> hub)
        {
            this._hub = hub;
            foreach (var hc in HitChecker.MakeHitChecker())
            {
                _hitChecker.Add(hc.ToString(), new BettingChecker()
                {
                    Betting =new List<BettingInfo>()
                    ,HitChecker = hc
                });
            }

            foreach(var num in Number.GetAllNumbers())
                randomBox.Add(100, num);

            updateWatch.Start();
        }

        Stopwatch updateWatch = new Stopwatch();
        void GameUpdate()
        {
            if(updateWatch.ElapsedMilliseconds > 10000)
            {
                updateWatch.Stop();

                int result = randomBox.Pick();
                var hitList = _hitChecker.Where(kvp => kvp.Value.HitChecker.IsHit(result));
                foreach(var kvp in hitList)
                {
                    int odds = kvp.Value.HitChecker.Odds + 1;
                    string why = "Win : " + kvp.Key;
                    foreach(var betting in kvp.Value.Betting)
                    {
                        var user = this._users[betting.UserId];
                        int gain = betting.Amount * odds;

                        MoneyChanged mc = new MoneyChanged()
                        {
                            Why = why,
                            Amount = gain,
                        };
                        if(user.CurrentBetting.Remove(betting) == false)
                        {
                        }
                        _hub.Clients.Client(user.UserId).SendAsync("MoneyChanged", mc);
                    }
                    kvp.Value.Betting.Clear();
                }

                foreach(var user in _users.Values)
                {
                    foreach(var betting in user.CurrentBetting)
                    {
                        MoneyChanged mc = new MoneyChanged()
                        {
                            Amount = -betting.Amount
                            , Why = "Lose : " + betting.BettingType
                        };
                        _hub.Clients.Client(user.UserId).SendAsync("MoneyChanged", mc);
                    }
                    user.CurrentBetting.Clear();
                }

                updateWatch.Restart();
            }
        }

        ulong totalBetting;
        int frame = 0;

        public Task ReceiveAsync(IContext context)
        {
            var msg = context.Message;

            if (msg is Update)
            {
                frame++;
                GameUpdate();
                context.Respond(0);
            }
            else if (msg is GetFrame)
            {
                context.Respond(++frame);
                frame = 0;
            }
            else if (msg is BettingInfo betting)
            {
                frame++;
                if (_hitChecker.TryGetValue(betting.BettingType, out var checker) == false)
                {
                    context.Respond(ApiResult.InvalidBetting);
                }
                checker.Betting.Add(betting);
                var user = _users[betting.UserId];
                user.Money -= betting.Amount;
                totalBetting += (ulong)betting.Amount;
                user.CurrentBetting.Add(betting);

                _hub.Clients.Client(user.UserId).SendAsync("OnBetting", betting);

                context.Respond(ApiResult.Success);
            }
            else if (msg is RequestNewUser newUser)
            {
                frame++;
                if (_users.TryGetValue(newUser.UserId, out var user) == false)
                {
                    user = new User()
                    {
                        UserId = newUser.UserId,
                        Money = 100000,
                        CurrentBetting = new List<BettingInfo>()
                    };
                    _users.Add(user.UserId, user);
                }
                _hub.Clients.Client(newUser.UserId).SendAsync("OnLogin", user);

                context.Respond(user);
            }
            else if (msg is SystemMessage) { }
            else
            {

            }

            return Actor.Done;
        }
        
        
    }

    public class RequestNewUser
    {
        public string UserId { get; set; }
    }
    
    public enum SessionEvent
    {
        None,
        OnLogin,
    }

    public class SessionMessage
    {
        public SessionEvent SessionEvent { get; set; }
        public string UserId { get; set; }
        public string ConnectionId { get; set; }
    }

    public class BettingChecker
    {
        public HitChecker HitChecker { get; set; }
        public List<BettingInfo> Betting { get; set; }
    }

}
