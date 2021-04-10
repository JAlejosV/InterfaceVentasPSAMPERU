using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// As informações gerais sobre um assembly são controladas por
// conjunto de atributos. Altere estes valores de atributo para modificar as informações
// associada a um assembly.
[assembly: AssemblyTitle("BusinessLogic")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("BusinessLogic")]
[assembly: AssemblyCopyright("Copyright ©  2019")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Definir ComVisible como false torna os tipos neste assembly invisíveis
// para componentes COM. Caso precise acessar um tipo neste assembly de
// COM, defina o atributo ComVisible como true nesse tipo.
[assembly: ComVisible(false)]

// O GUID a seguir será destinado à ID de typelib se este projeto for exposto para COM
[assembly: Guid("a2826ede-e7be-4f86-b529-af889d903fc1")]

// As informações da versão de um assembly consistem nos quatro valores a seguir:
//
//      Versão Principal
//      Versão Secundária 
//      Número da Versão
//      Revisão
//
// É possível especificar todos os valores ou usar como padrão os Números de Build e da Revisão
// usando o '*' como mostrado abaixo:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

//Log4Net
[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config")]
//[assembly: log4net.Config.XmlConfigurator(ConfigFile = "App.config", Watch = true)]