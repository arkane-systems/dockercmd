using System;
using System.Collections.Generic;
using System.Text;

namespace ArkaneSystems.DockerCmd
{
    internal class CommandDefinition
    {
        public string Image { get ; set ; }

        public string Name { get ; set ; }

        public bool Interactive { get ; set ; } = true ;

        public bool PersistContainer { get ; set ; } = false ;
    }
}
