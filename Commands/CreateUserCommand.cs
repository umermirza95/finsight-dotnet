namespace Finsight.Commands
{
    public class CreateFSUserCommand()
    {
        public required string Email { get; set; }
        public required string Password { get; set; } 
        public required string Name { get; set; } 
    }    
}