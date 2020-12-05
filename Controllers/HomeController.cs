namespace Polly_Sample_Code.Controllers
{
    using System;
    using System.Linq;
    using System.Text;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Polly;

    [Route("[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;
        private Polly.CircuitBreaker.CircuitBreakerPolicy _circuitBreakerPolicy;
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;

            _circuitBreakerPolicy = Policy
           .Handle<Exception>()
           .CircuitBreaker(exceptionsAllowedBeforeBreaking: 10,
               durationOfBreak: TimeSpan.FromMinutes(10));
        }

        [HttpGet("Retry/{retry}")]
        public IActionResult RetryExample(int retry)
        {
            var result = new StringBuilder();
            var retryPolicy = Policy.Handle<Exception>().Retry(retry, (e, r) =>
            {
                result.AppendFormat("Retry : {0} , Exception Message : {1}", r, e.Message).AppendLine();
                _logger.LogWarning("Retrying : {0}", r);
            });

            var counter = 0;
            retryPolicy.Execute(() =>
            {
                counter++;
                if (counter < retry) throw new Exception("Throwing a fake error");
            });

            result.Append("RetryExample completed").AppendLine();
            return Ok(result.ToString());
        }

        [HttpGet("RetryWithDelay/{retry}/{delay}")]
        public IActionResult RetryWithDelay(int retry, int delay)
        {
            var result = new StringBuilder();

            var retryAndWaitArgs = Enumerable.Range(0, retry).Select(v => delay * v).ToArray();

            var retryPolicyWithDelay = Policy.Handle<Exception>().WaitAndRetry(retry, r =>
            {
                var returnValue = TimeSpan.FromSeconds(r * delay);
                result.AppendFormat("running retry :{0}", DateTime.Now.AddTicks(returnValue.Ticks)).AppendLine();
                return returnValue;

            });

            var counter = 0;
            retryPolicyWithDelay.Execute(() =>
            {
                counter++;
                if (counter < retry) throw new Exception("Throwing a fake error");
            });

            result.Append("RetryWithDelay completed").AppendLine();
            return Ok(result.ToString());
        }



        static int _counter = 0;
        static StringBuilder _circuitBreakerResult = new StringBuilder();

        
        [HttpGet("CircuitBreak")]
        public IActionResult CircuitBreakerExample()
        {
            try
            {
                _circuitBreakerPolicy.Execute(() =>
                            {
                                _circuitBreakerResult.AppendFormat("Creating fake error {0}", _counter).AppendLine();
                                _counter++;
                                if (_counter < 3) throw new Exception("Throwing a fake error" + _counter);
                            });

                _circuitBreakerResult.Append("CircuitBreak completed").AppendLine();

                var str = _circuitBreakerResult.ToString();

                // reset this...
                _counter = 0;
                _circuitBreakerResult = new StringBuilder();
                return Ok(str);
            }
            catch (Exception e)
            {
                _logger.LogInformation(e.Message);
            }

            return Ok("error occured :" + _counter);
        }
    }
}
