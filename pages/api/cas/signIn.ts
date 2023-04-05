import { withIronSessionApiRoute } from "iron-session/next";
import { NextApiRequest, NextApiResponse } from "next";
import { casV1Users } from "~/lib/cas";
import { sessionOptions } from "~/lib/session";

async function handler(request: NextApiRequest, response: NextApiResponse) {
  try {
    const userInfo = await casV1Users(request.body.username, request.body.password)
    const attr = userInfo["authentication"]["successes"]["RestAuthenticationHandler"]["principal"]["attributes"]
    request.session.user = {
        verified: true,
        role: attr.identity,
        realName: attr.name,
        organization: attr.organization,
        casId: request.body.username,
        casPassword: request.body.password
    }
    await request.session.save();
    response.redirect(302, '/')
  } catch (error) {
    response.status(500).json({ message: (error as Error).message });
  }
}

export default withIronSessionApiRoute(handler, sessionOptions);
