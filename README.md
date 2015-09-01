## Pay Insights SDK - Samples ##

# Overview - PI-SDK and the Platform

**PI** stands for **Pay Insights**.

**PI Platform** was created to easy the acceptance and processing of card transactions, abstracting several concerns like **security**, **EMV**, **provisioning**, and any technical requirements and let the developer only to responsibility to develop their payment application with focus on its business not on financial transaction caveats. The platform is fully **PCI-compliant**, and is hosted on **Microsoft Azure**, giving the power of scalability to the app developer and the card acceptor. With that in mind, the transaction power/processing is virtually unlimited. The platform was build with the customer in mind. Each customer has its own transaction/business process and with that in mind, we the platform has a built-in Business Engine, where you can work together with Pay Insights team and have your own transaction process made exclusive for your business! If you just want to accept financial transactions on your application, we have a pre-built transaction process that is the default one when you start using the services and it works for 99% of the customers whiling to accept credit/debit cards on your systems.

**PI SDK** is a very easy SDK, with a small set of APIs supported across several platforms and terminal devices. The SDK is used to communicate with the server side of the platforms, which handles the heavy lift of transaction processing. You just have to worry about create the best app for your business and we will ensure that you have the best transaction experience on the market no matter if its an EMV, Magnetic, Contactless, terminal, checkout, vending machine, cell phone, e-commerce, whatever! **No matter the way you want to accept it, we will make it work thru the SDK!** (for more details and/or customization, please get in touch with us at [hello@payinsights.com](mailto:hello@payinsights.com))

Well, enough chat, let's see how it works!


# Getting started with PI-SDK

In this repo, you will find samples to all platforms currently supported by the SDK. 

In order to use the SDK, you must follow this 6 simple steps:

**1. Install PI.SDK Nuget Package**

From the Package Manager console, run the following command:

    PM> Install-Package PI.SDK 

You can also install the Nuget Package from Visual Studio or Xamarin Studio IDEs.

**2. Get your ApplicationId, ApplicationSecret and CustomerId**

Contact [hello@payinsights.com](mailto:hello@payinsights.com) and get your credentials for development. Let us know how you are going to use the platform on your application, we would love to hear from you! 

Those Development credentials uses test servers that are shared with other many other people, but rest assured that the data you used at the transaction, are safely transmitted and only you have access to it. For production deployment or if for some reason you requires a dedicated Dev instance, please let us know.

**3. Initialize the main SDK objects.**

You have to initialize the device channel, the device manager and the Platform Context. They are core concepts of the SDK, and you can see a deep dive explanation of each one of those on the BasicConsole sample.

     var channel = new SerialDeviceConnectionChannel(_port);
     var deviceManager = new BCDeviceManager(channel);
     PlatformContext.Initialize(_customerId, _appId, _appSecret, _deviceManager, PIEnvironment.Dev);

**4. Acquire an access token**


    var token = PlatformContext.GetService<IAuthService>().Login();


**5. Setup Transaction Orchestrator**

This guy is responsible to initiate and handle many events of the transaction process.
    
    var transactionOrchestrator = PlatformContext.GetService<ITransactionOrchestrator>();
    transactionOrchestrator.OnError += OnError;
    transactionOrchestrator.OnProgressChanged += TransactionOrchestratorOnProgressChanged;
    transactionOrchestrator.OnTransactionFinished += TransactionOrchestratorOnTransactionFinished;
    transactionOrchestrator.OnDataRequested += TransactionOrchestratorOnDataRequested;


**6. Execute the transaction (the best part!)**

    var parameters = new TerminalTransactionParameters
    {
    	Mode = TransactionMode.Credit,
    	Type = TransactionType.Authorization,
    	Amount = 20,
    	Parcel = 1
    };
    transactionOrchestrator.ExecuteTransaction(parameters, _token); 

An that is it!

Assuming that you have a connected serial terminal/pinpad, you will be able to make transactions with EMV, Magnetic or Contactless cards! Noto that you don't see a single line of internal communication with the device, neither specific EMV implementation. All you have to do now, is to implement your payment application and let us take care of those details.

This code is extracted from the BasicConsole sample, where you will have full details about each of those lines. The same code applies to all other supported platforms. 

# Wrapping up #

Here you can see how easy is to perform a card present transaction using **PI-SDK**. You can perform e-commerce or any other kind of transactions as well.

More samples and updates to come in the coming weeks.

[]s

**Pay Insights Team**