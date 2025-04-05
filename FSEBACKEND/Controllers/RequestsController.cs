using Microsoft.AspNetCore.Mvc;
using FSEBACKEND.Data;
using FSEBACKEND.Models;
using Microsoft.EntityFrameworkCore;

namespace FSEBACKEND.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RequestsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RequestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /requests
        [HttpGet]
        public async Task<IActionResult> GetRequests()
        {
            var requests = await _context.Requests.ToListAsync();
            return Ok(requests);
        }

        // GET: /requests/unaccepted/{email}
        [HttpGet("unaccepted/{email}")]
        public async Task<IActionResult> GetUnacceptedRequests(string email)
        {
            var requests = await _context.Requests
                .Where(r => r.Email == email && !r.Accepted)
                .ToListAsync();
            return Ok(requests);
        }

        // GET: /requests/accepted/{email}
        [HttpGet("accepted/{email}")]
        public async Task<IActionResult> GetAcceptedRequests(string email)
        {
            var requests = await _context.Requests
                .Where(r => r.Email == email && r.Accepted)
                .ToListAsync();
            return Ok(requests);
        }

        // PUT: /requests/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRequest(int id, [FromBody] Request updatedRequest)
        {
            var existingRequest = await _context.Requests.FindAsync(id);
            if (existingRequest == null)
            {
                return NotFound("Request not found.");
            }

            existingRequest.Accepted = updatedRequest.Accepted;
            _context.Requests.Update(existingRequest);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Request updated successfully." });
        }

        // DELETE: /requests/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            var request = await _context.Requests.FindAsync(id);
            if (request == null)
            {
                return NotFound("Request not found.");
            }

            _context.Requests.Remove(request);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Request deleted successfully." });
        }

        // POST: /addrequest
        [HttpPost("/addrequest")]
        public async Task<IActionResult> CreateRequest([FromBody] Request newRequest)
        {
            if (newRequest == null || newRequest.BookId <= 0 || string.IsNullOrEmpty(newRequest.Email))
            {
                return BadRequest("Invalid request data.");
            }

            _context.Requests.Add(newRequest);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Request created successfully." });
        }
    }
}