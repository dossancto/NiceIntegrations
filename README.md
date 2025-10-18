# NiceIntegrations

Este repositório contém o código fonte para o post do blog [Como desenvolver integrações](https://blog.dossancto.com.br/blog/integracoes/), que discute as melhores práticas para implementar integrações de serviços externos em aplicações .NET modernas.

O objetivo principal é estruturar as integrações de forma a minimizar as dependências de serviços externos, facilitando a migração de um provedor para outro e simplificando o processo de escrita de testes unitários e de integração.

> [!INFO]
> Este projeto requer **.NET 7** ou superior. **.NET 8 ou 9** é preferível devido ao atributo nativo `[FromKeyedServices]` e outras melhorias para a DI de Serviços Chaveados (Keyed Service).

## Introdução

Vamos explorar quatro casos de uso comuns para integrações, usando uma aplicação de E-commerce como exemplo:

1.  **Provedor Único**: Para serviços com apenas um provedor durante todo o ciclo de vida da aplicação.
2.  **Provedores Dependentes do Contexto**: Para quando o provedor varia dependendo do contexto atual (ex: manipular um pedido de um marketplace específico).
3.  **Broadcast para Múltiplos Provedores**: Para quando uma ação precisa ser despachada para múltiplos provedores simultaneamente (ex: desativar um produto em todos os marketplaces integrados).
4.  **Provedores como Fallback**: Para quando você precisa tentar uma série de provedores em ordem até que um tenha sucesso (ex: gateways de pagamento ou serviços de envio de email).

## Padrões de Integração

Os padrões são ordenados pela minha percepção de complexidade de implementação.

### 1. Provedor Único

Este é o caso de uso mais simples, onde sua aplicação depende de um único provedor para uma tarefa específica.

A melhor prática é definir uma interface que abstraia a funcionalidade do serviço, garantindo que sua camada de domínio permaneça desacoplada de recursos externos. Por exemplo, em vez de criar uma interface ligada a um provedor específico como `ISendGridService`, defina uma genérica como `IEmailSenderAdapter`.

Então, implemente esta interface em uma classe específica do provedor, como `SendGridEmailProvider`. Essa abordagem torna o serviço modular, facilmente substituível e melhora a clareza do código.

### 2. Provedores Dependentes do Contexto

Este padrão é útil quando você precisa interagir com diferentes provedores do mesmo tipo com base em um determinado contexto. Por exemplo, em uma plataforma de e-commerce, você pode precisar atualizar o status de um pedido no Rappi ou no iFood, dependendo de onde o pedido se originou.

Primeiro, defina uma interface comum, como `IMarketplaceAdapter`, e crie implementações separadas para cada provedor (ex: `RappiMarketplaceProvider`, `IFoodMarketplaceProvider`).

Como várias classes implementam a mesma interface, você não pode usar a injeção de dependência padrão via construtor. Em vez disso, você registra cada implementação como um **Serviço Chaveado (Keyed Service)**.

**Registrando Serviços Chaveados:**

Você pode registrar serviços com uma chave específica durante a configuração da injeção de dependência. Aqui está um exemplo deste repositório onde diferentes remetentes de email são registrados com chaves correspondentes a um `enum`:

```csharp
// Em Modules/Commum/SubModules/EmailSenders/EmailSendersModule.cs

private static IServiceCollection AddLoggerEmailSenderAdapter(this IServiceCollection services)
{
    services.AddKeyedTransient<IEmailSenderPort, LoggerEmailSenderAdapter>(EmailSenderType.Logger.ToString());
    AvailibleEmailSenders.Add(EmailSenderType.Logger);
    return services;
}

private static IServiceCollection AddMailgunEmailSenderAdapter(this IServiceCollection services)
{
    services.AddHttpClient<MailGunEmailAdapter>();
    services.AddKeyedTransient<IEmailSenderPort, MailGunEmailAdapter>(EmailSenderType.Mailgun.ToString());
    AvailibleEmailSenders.Add(EmailSenderType.Mailgun);
    return services;
}
```

**Resolvendo Serviços Dinamicamente:**

Você pode então resolver o serviço desejado dinamicamente em tempo de execução, criando um escopo de serviço e usando a chave. A chave pode vir de um DTO, uma propriedade de objeto de domínio ou qualquer outro dado contextual.

```csharp
// Em Modules/Commum/SubModules/EmailSenders/Adapters/EmailSenderManager.cs

using var scope = _scopeFactory.CreateScope();

// A variável 'sender' contém a chave, ex: "Logger" ou "Mailgun"
var adapter = scope.ServiceProvider.GetKeyedService<IEmailSenderPort>(sender.ToString());

if (adapter is not null)
{
    // Use o adaptador...
}
```

Essa abordagem garante que seu código de chamada permaneça limpo e inalterado, mesmo ao adicionar novos provedores.

### 3. Broadcast para Múltiplos Provedores

Este padrão é para cenários onde você precisa enviar uma mensagem ou disparar uma ação em múltiplos provedores de uma só vez. Por exemplo, quando o nível de estoque de um produto cai para zero, você pode precisar desativá-lo em vários marketplaces diferentes.

Para implementar isso, registre todas as implementações de provedor com a **mesma chave**.

Você pode então injetar um `IEnumerable<IMarketplaceProductManagerAdapter>` para obter todos os serviços registrados para essa chave.

```csharp
[ApiController]
public class MyController : ControllerBase
{
    private readonly IEnumerable<IMarketplaceProductManagerAdapter> _productManagers;

    // .NET 8+ permite injeção direta com [FromKeyedServices]
    public MyController([FromKeyedServices("orders")] IEnumerable<IMarketplaceProductManagerAdapter> productManagers)
    {
        _productManagers = productManagers;
    }
}
```

Crie uma classe `Handler` que receba este `IEnumerable` e itere sobre todos os provedores, executando a ação desejada. Garanta que uma falha em um provedor não impeça que os outros sejam executados. Registre avisos (warnings) para quaisquer falhas e considere lançar uma `AggregateException` se todos os provedores falharem.

### 4. Provedores como Fallback

Este padrão envolve tentar múltiplos provedores sequencialmente até que um complete a tarefa com sucesso. É ideal para cenários de alta disponibilidade onde o provedor específico usado não é importante para o usuário final, como envio de emails ou processamento de pagamentos.

Este repositório fornece um exemplo claro deste padrão para o envio de emails.

**Implementação:**

1.  **Registrar Provedores**: Registre múltiplas implementações de `IEmailSenderPort` como serviços chaveados, como mostrado na seção "Provedores Dependentes do Contexto".
2.  **Criar um Gerenciador**: Crie uma classe de gerenciamento/handler (`EmailSenderManager`) que também implementa `IEmailSenderPort`, mas **não** é registrada como um serviço chaveado. Este gerenciador é responsável por orquestrar a lógica de fallback.
3.  **Implementar a Lógica de Fallback**: O gerenciador itera através dos provedores disponíveis, tentando a operação com each um. Se uma tentativa for bem-sucedida, ele retorna imediatamente. Se falhar, ele registra um aviso (warning) e continua para o próximo provedor. Se todos os provedores falharem, ele lança uma `AggregateException`.

Aqui está a implementação de `EmailSenderManager.cs`:

```csharp
public class EmailSenderManager(
    IServiceScopeFactory scopeFactory,
    ILogger<EmailSenderManager> logger
) : IEmailSenderPort
{
    public Task<SendSimpleEmailResponse> SendSimpleEmailAsync(SendSimpleEmailRequest req, CancellationToken cancellationToken = default)
    {
        if (EmailSendersModule.AvailibleEmailSenders.Count is 0)
        {
            throw new ArgumentException("No adapter found");
        }

        using var scope = _scopeFactory.CreateScope();
        var exceptions = new List<Exception>();

        // EmailSendersModule.AvailibleEmailSenders contém as chaves de todos os provedores registrados
        foreach (var senderKey in EmailSendersModule.AvailibleEmailSenders)
        {
            var adapter = scope.ServiceProvider.GetKeyedService<IEmailSenderPort>(senderKey.ToString());
            if (adapter is null) continue;

            try
            {
                // Tenta a operação
                var response = adapter.SendSimpleEmailAsync(req, cancellationToken);
                return response; // Sucesso, retorna imediatamente
            }
            catch (Exception ex)
            {
                // Falha, loga e tenta o próximo
                exceptions.Add(ex);
                _logger.LogWarning(ex, "Fail while sending email with {Sender}", senderKey);
            }
        }

        // Se todos os provedores falharam
        throw new AggregateException("All email sender providers failed.", exceptions);
    }
}
```

Este modelo torna seu sistema mais resiliente. Você pode até embaralhar a lista de provedores para distribuir a carga e os custos entre diferentes serviços. Adicionar um novo provedor requer apenas a implementação da interface e seu registro—nenhuma alteração é necessária no código de chamada.