using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Logic.Tests
{
    public sealed class CommandDispatcherTest
    {
        private const float OmittedSeconds = -1f;

        private CommandDispatcher _dispatcher;
        private string _target;
        private float _seconds;
        private int _calls;

        [SetUp]
        public void SetUp()
        {
            _calls = 0;
            _target = null;
            _seconds = 0f;
            _dispatcher = new CommandDispatcher();
            _dispatcher.RegisterCommandHandler<string, float>("move", Move);
        }

        [Test]
        public void Dispatch_OptionalArgumentOmitted_ShouldFillItWithTheDefaultValue()
        {
            Dispatch("move door");

            Assert.That(_calls, Is.EqualTo(1));
            Assert.That(_target, Is.EqualTo("door"));
            Assert.That(_seconds, Is.EqualTo(OmittedSeconds));
        }

        [Test]
        public void Dispatch_OptionalArgumentWritten_ShouldUseTheWrittenValue()
        {
            Dispatch("move door 2");

            Assert.That(_seconds, Is.EqualTo(2f));
        }

        [Test]
        public void Dispatch_MoreArgumentsThanParameters_ShouldFailWithoutCallingTheHandler()
        {
            LogAssert.Expect(LogType.Error, new Regex("expects 1 to 2 arguments, but 3 were provided"));

            Dispatch("move door 2 3");

            Assert.That(_calls, Is.Zero);
        }

        [Test]
        public void Dispatch_RequiredArgumentMissing_ShouldFailWithoutCallingTheHandler()
        {
            LogAssert.Expect(LogType.Error, new Regex("expects 1 to 2 arguments, but 0 were provided"));

            Dispatch("move");

            Assert.That(_calls, Is.Zero);
        }

        private void Dispatch(string command) => _dispatcher.DispatchCommandAsync(command, CancellationToken.None).GetAwaiter().GetResult();

        private void Move(string target, float seconds = OmittedSeconds)
        {
            _calls++;
            _target = target;
            _seconds = seconds;
        }
    }
}
