using Asp.Versioning;
using IHCShared;
using IHCShared.IHCController;
using Microsoft.AspNetCore.Mvc;
using Utilities.MessageQueue;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace IHC_WebApi.Controllers
{
    [Route("api/v{version:apiversion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class IHCTerminalsController(
        IHCCache ihcCache,
        IMessageQueue<IHCClientCommand> messageQueue) 
        : ControllerBase, IIHCTerminalsController
    {
        /// <summary>
        /// Retrieves all IHC terminals.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> containing a collection of <see cref="IHCModule"/> objects with HTTP status
        /// code 200 (OK). The collection will be empty if no modules are available.</returns>
        [HttpGet]
        [Route("[action]")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType<IEnumerable<IHCModule>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllModules()
        {
            var terminals = ihcCache.GetAllModules();
            return Ok(terminals);
        }

        /// <summary>
        /// Retrieves all IHC input terminals.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> containing a collection of <see cref="IHCModule"/> objects representing all
        /// input modules. Returns a 200 OK response with the collection; the collection will be empty if no input
        /// terminals are found.</returns>
        [HttpGet]
        [Route("[action]")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType<IEnumerable<IHCModule>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllInputModules()
        {
            var terminals = ihcCache.GetAllModules(IHCType.Input);
            return Ok(terminals);
        }

        /// <summary>
        /// Retrieves all IHC output terminals.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> containing a collection of <see cref="IHCModule"/> objects representing all
        /// output modules. Returns a 200 OK response with the collection.</returns>
        [HttpGet]
        [Route("[action]")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType<IEnumerable<IHCModule>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllOutputModules()
        {
            var terminals = ihcCache.GetAllModules(IHCType.Output);
            return Ok(terminals);
        }

        /// <summary>
        /// Retrieves all IHC terminals.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> containing a collection of <see cref="IHCTerminal"/> objects with HTTP status
        /// code 200 (OK). The collection will be empty if no terminals are available.</returns>
        [HttpGet]
        [Route("[action]")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType<IEnumerable<IHCTerminal>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllTerminals()
        {
            var terminals = ihcCache.GetAllTerminals();
            return Ok(terminals);
        }

        /// <summary>
        /// Retreives all terminals which match the terminal type and module number
        /// </summary>
        /// <param name="content">A <see cref="TerminalsByTypeAndModuleNumberRequest"/></param>
        /// <returns>An <see cref="IActionResult"/> containing a collection of <see cref="IHCTerminal"/> objects with HTTP status
        /// code 200 (OK). The collection will be empty if no terminals are available.</returns>
        [HttpGet]
        [Route("[action]")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType<IEnumerable<IHCTerminal>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTerminals(TerminalsByTypeAndModuleNumberRequest content)
        {
            var terminals = ihcCache.GetAllTerminals();

            IEnumerable<IHCTerminal> outTerminals = terminals.Where<IHCTerminal>(t =>  t.TerminalType == content.TerminalType && t.ModuleNumber == content.ModuleNumber);
            return Ok(outTerminals);
        }

        /// <summary>
        /// Retrieves all IHC input terminals.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> containing a collection of <see cref="IHCTerminal"/> objects representing all
        /// input terminals. Returns a 200 OK response with the collection; the collection will be empty if no input
        /// terminals are found.</returns>
        [HttpGet]
        [Route("[action]")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType<IEnumerable<IHCTerminal>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllInputTerminals()
        {
            var terminals = ihcCache.GetAllTerminals(IHCType.Input);
            return Ok(terminals);
        }

        /// <summary>
        /// Retrieves all IHC output terminals.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> containing a collection of <see cref="IHCTerminal"/> objects representing all
        /// output terminals. Returns a 200 OK response with the collection.</returns>
        [HttpGet]
        [Route("[action]")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType<IEnumerable<IHCTerminal>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllOutputTerminals()
        {
            var terminals = ihcCache.GetAllTerminals(IHCType.Output);
            return Ok(terminals);
        }

        /// <summary>
        /// Retrieves the input terminal associated with the specified controller number.
        /// </summary>
        /// <param name="controllerNumber">The controller number of the terminal to retrieve.</param>
        /// <returns>An <see cref="IActionResult"/> containing the input terminal if found; otherwise, a 404 Not Found response.</returns>
        [HttpGet]
        [Route("[action]")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType<IEnumerable<IHCTerminal>>(StatusCodes.Status200OK)]
        [ProducesResponseType<IHCTerminal>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInputTerminal(ControllerNumberRequest content)
        {
            var terminal = ihcCache.GetTerminal(IHCType.Input, content.ControllerNumber);
            if (terminal == null)
            {
                return NotFound();
            }
            return Ok(terminal);
        }

        /// <summary>
        /// Retrieves the output terminal associated with the specified controller number.
        /// </summary>
        /// <param name="controllerNumber">The controller number of the terminal to retrieve.</param>
        /// <returns>An <see cref="IActionResult"/> containing the output terminal if found; otherwise, a 404 Not Found response.</returns>
        [HttpGet]
        [Route("[action]")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType<IEnumerable<IHCTerminal>>(StatusCodes.Status200OK)]
        [ProducesResponseType<IHCTerminal>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOutputTerminal([FromBody] ControllerNumberRequest content)
        {
            var terminal = ihcCache.GetTerminal(IHCType.Output, content.ControllerNumber);
            if (terminal == null)
            {
                return NotFound();
            }
            return Ok(terminal);
        }

        /// <summary>
        /// Activates the input terminal associated with the specified controller number.
        /// </summary>
        /// <param name="controllerNumber">The controller number of the terminal to activate.</param>
        /// <returns>An <see cref="NoContentResult"/> if the input terminal is successfully activated; otherwise, a <see
        /// cref="NotFoundResult"/> if the specified controller number does not correspond to an existing terminal.</returns>
        [HttpPut]
        [Route("[action]")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType<IEnumerable<IHCTerminal>>(StatusCodes.Status204NoContent)]
        [ProducesResponseType<IHCTerminal>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActivateInput(ControllerNumberRequest content)
        {
            var terminal = ihcCache.GetTerminal(IHCType.Output, content.ControllerNumber);
            if (terminal == null)
            {
                return NotFound();
            }

            IHCClientCommand iHCCommand = new(IHCClientCommandType.ActivateInput, IHCType.Input, content.ControllerNumber);
            await messageQueue.EnqueueAsync(iHCCommand);

            return NoContent();
        }

        /// <summary>
        /// Sets the state of the specified output terminal on the IHC controller.
        /// </summary>
        /// <param name="controllerNumber">The controller number of the output terminal to update.</param>
        /// <param name="state">The desired state to set for the output terminal. Set to <see langword="true"/> to turn the output on;
        /// otherwise, <see langword="false"/>.</param>
        /// <returns>An <see cref="NoContentResult"/> if the output terminal state is successfully updated; otherwise, a <see
        /// cref="NotFoundResult"/> if the specified controller number does not correspond to an existing terminal.</returns>
        [HttpPut]
        [Route("[action]")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType<IEnumerable<IHCTerminal>>(StatusCodes.Status204NoContent)]
        [ProducesResponseType<IHCTerminal>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetOutputState(SetOutputStateRequest content)
        {
            var terminal = ihcCache.GetTerminal(IHCType.Output, content.ControllerNumber);
            if (terminal == null)
            {
                return NotFound();
            }

            IHCClientCommand iHCCommand = new(IHCClientCommandType.SetOutputState, IHCType.Output, content.ControllerNumber, content.State);
            await messageQueue.EnqueueAsync(iHCCommand);

            return NoContent();
        }

        /// <summary>
        /// Retrieves the total number of output modules available in the system.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> that contains the total count of output modules as an unsigned integer with
        /// an HTTP 200 status code.</returns>
        [HttpGet]
        [Route("[action]")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType<uint>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOutputModulesCount()
        {
            var count = ihcCache.GetModulesCount(IHCType.Output);
            return Ok(count);
        }

        /// <summary>
        /// Retrieves the total number of input modules available in the system.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> that contains the total count of input modules as an unsigned integer with an
        /// HTTP 200 status code.</returns>
        [HttpGet]
        [Route("[action]")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType<uint>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInputModulesCount()
        {
            var count = ihcCache.GetModulesCount(IHCType.Input);
            return Ok(count);
        }
    }
}
