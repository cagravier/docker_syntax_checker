namespace DockerSyntaxChecker
{
  public enum InstructionType
   {
       Unknown,
       FROM,
       SYNTAX,
       ESCAPE,
       ARG,
       COMMENT,
       ENV,
       ONBUILD,
       STOPSIGNAL,
       USER,
       VOLUME,
       WORKDIR
   }
 }
